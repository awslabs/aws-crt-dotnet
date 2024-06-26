/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
#include "crt.h"
#include "exports.h"
#include "http_client.h"
#include "stream.h"

#include <aws/auth/credentials.h>
#include <aws/auth/signable.h>
#include <aws/auth/signing.h>
#include <aws/auth/signing_config.h>
#include <aws/auth/signing_result.h>
#include <aws/cal/ecc.h>
#include <aws/common/string.h>
#include <aws/http/request_response.h>
#include <aws/io/stream.h>

typedef bool(DOTNET_CALL aws_dotnet_auth_should_sign_header_fn)(uint8_t *header_name, int32_t header_name_length);

struct aws_signing_config_native {
    int32_t algorithm;

    int32_t signature_type;

    const char *region;

    const char *service;

    int64_t milliseconds_since_epoch;

    const char *access_key_id;

    const char *secret_access_key;

    const char *session_token;

    aws_dotnet_auth_should_sign_header_fn *should_sign_header;

    uint8_t use_double_uri_encode;

    uint8_t should_normalize_uri_path;

    uint8_t omit_session_token;

    const char *signed_body_value;

    int32_t signed_body_header;

    uint64_t expiration_in_seconds;
};

typedef void(DOTNET_CALL aws_dotnet_auth_on_signing_complete_fn)(
    uint64_t callback_id,
    int32_t error_code,
    const uint8_t *signature,
    uint64_t signature_size,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count);

struct aws_dotnet_signing_callback_state {
    struct aws_http_message *request;
    struct aws_input_stream *body_stream;
    struct aws_signable *original_request_signable;
    struct aws_credentials *credentials;
    struct aws_string *region;
    struct aws_string *service;
    struct aws_string *signed_body_value;
    aws_dotnet_auth_should_sign_header_fn *should_sign_header;
    uint64_t callback_id;
    aws_dotnet_auth_on_signing_complete_fn *on_signing_complete;
    enum aws_signature_type signature_type;
};

static void s_destroy_signing_callback_state(struct aws_dotnet_signing_callback_state *callback_state) {
    if (callback_state == NULL) {
        return;
    }

    aws_credentials_release(callback_state->credentials);
    aws_signable_destroy(callback_state->original_request_signable);
    aws_string_destroy(callback_state->region);
    aws_string_destroy(callback_state->service);
    aws_string_destroy(callback_state->signed_body_value);
    aws_input_stream_release(callback_state->body_stream);
    aws_http_message_release(callback_state->request);

    aws_mem_release(aws_dotnet_get_allocator(), callback_state);
}

static struct aws_byte_cursor s_byte_cursor_from_nullable_c_string(const char *string) {
    struct aws_byte_cursor cursor;
    AWS_ZERO_STRUCT(cursor);

    if (string != NULL) {
        cursor.ptr = (uint8_t *)string;
        cursor.len = strlen(string);
    }

    return cursor;
}

static bool s_should_sign_header_adapter(const struct aws_byte_cursor *name, void *user_data) {
    struct aws_dotnet_signing_callback_state *callback_state = user_data;

    return callback_state->should_sign_header(name->ptr, (int32_t)name->len);
}

static int s_initialize_signing_config(
    struct aws_signing_config_aws *config,
    struct aws_signing_config_native *dotnet_config,
    struct aws_dotnet_signing_callback_state *callback_state) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    config->config_type = AWS_SIGNING_CONFIG_AWS;
    config->algorithm = dotnet_config->algorithm;
    config->signature_type = dotnet_config->signature_type;

    if (dotnet_config->region != NULL) {
        callback_state->region = aws_string_new_from_c_str(allocator, dotnet_config->region);
        config->region = aws_byte_cursor_from_string(callback_state->region);
    }

    if (dotnet_config->service != NULL) {
        callback_state->service = aws_string_new_from_c_str(allocator, dotnet_config->service);
        config->service = aws_byte_cursor_from_string(callback_state->service);
    }

    aws_date_time_init_epoch_millis(&config->date, (uint64_t)dotnet_config->milliseconds_since_epoch);

    config->flags.use_double_uri_encode = dotnet_config->use_double_uri_encode != 0;
    config->flags.should_normalize_uri_path = dotnet_config->should_normalize_uri_path != 0;
    config->flags.omit_session_token = dotnet_config->omit_session_token != 0;

    callback_state->credentials = aws_credentials_new(
        allocator,
        s_byte_cursor_from_nullable_c_string(dotnet_config->access_key_id),
        s_byte_cursor_from_nullable_c_string(dotnet_config->secret_access_key),
        s_byte_cursor_from_nullable_c_string(dotnet_config->session_token),
        UINT64_MAX);
    if (callback_state->credentials == NULL) {
        aws_raise_error(AWS_AUTH_SIGNING_INVALID_CONFIGURATION);
        return AWS_OP_ERR;
    }

    config->signed_body_header = dotnet_config->signed_body_header;

    if (dotnet_config->signed_body_value != NULL) {
        callback_state->signed_body_value = aws_string_new_from_c_str(allocator, dotnet_config->signed_body_value);
        if (callback_state->signed_body_value == NULL) {
            return AWS_OP_ERR;
        }

        config->signed_body_value = aws_byte_cursor_from_c_str((const char *)callback_state->signed_body_value->bytes);
    }

    config->credentials = callback_state->credentials;

    config->expiration_in_seconds = dotnet_config->expiration_in_seconds;

    if (dotnet_config->should_sign_header != NULL) {
        config->should_sign_header = s_should_sign_header_adapter;
        config->should_sign_header_ud = callback_state;
    }

    return AWS_OP_SUCCESS;
}

static void s_complete_signing_exceptionally(struct aws_dotnet_signing_callback_state *callback_state, int error_code) {
    callback_state->on_signing_complete(callback_state->callback_id, error_code, NULL, 0, NULL, NULL, 0);
}

static void s_complete_http_request_signing_normally(
    struct aws_dotnet_signing_callback_state *callback_state,
    struct aws_string *signature) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    struct aws_byte_cursor path_cursor;
    AWS_ZERO_STRUCT(path_cursor);

    aws_http_message_get_request_path(callback_state->request, &path_cursor);
    struct aws_string *uri = aws_string_new_from_cursor(allocator, &path_cursor);

    size_t header_count = aws_http_message_get_header_count(callback_state->request);
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_dotnet_http_header, dotnet_headers, header_count);

    for (size_t header_idx = 0; header_idx < header_count; ++header_idx) {
        AWS_ZERO_STRUCT(dotnet_headers[header_idx]);

        struct aws_http_header header;
        AWS_ZERO_STRUCT(header);

        if (aws_http_message_get_header(callback_state->request, &header, header_idx)) {
            continue;
        }

        dotnet_headers[header_idx].name = (const char *)header.name.ptr;
        dotnet_headers[header_idx].name_size = header.name.len;
        dotnet_headers[header_idx].value = (const char *)header.value.ptr;
        dotnet_headers[header_idx].value_size = header.value.len;
    }

    callback_state->on_signing_complete(
        callback_state->callback_id,
        AWS_ERROR_SUCCESS,
        signature->bytes,
        signature->len,
        (const char *)uri->bytes,
        dotnet_headers,
        (uint32_t)header_count);

    aws_string_destroy(uri);
}

static void s_complete_http_request_signing(
    struct aws_dotnet_signing_callback_state *callback_state,
    struct aws_signing_result *result) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    if (aws_apply_signing_result_to_http_request(callback_state->request, allocator, result)) {
        s_complete_signing_exceptionally(callback_state, aws_last_error());
        return;
    }

    struct aws_string *authorization_result = NULL;
    aws_signing_result_get_property(result, g_aws_signature_property_name, &authorization_result);
    if (authorization_result == NULL) {
        s_complete_signing_exceptionally(callback_state, AWS_ERROR_INVALID_STATE);
        return;
    }

    s_complete_http_request_signing_normally(callback_state, authorization_result);
}

static void s_complete_signature_signing(
    struct aws_dotnet_signing_callback_state *callback_state,
    struct aws_signing_result *result) {

    struct aws_string *authorization_result = NULL;
    aws_signing_result_get_property(result, g_aws_signature_property_name, &authorization_result);

    if (authorization_result == NULL) {
        s_complete_signing_exceptionally(callback_state, AWS_ERROR_INVALID_STATE);
    } else {
        callback_state->on_signing_complete(
            callback_state->callback_id, 0, authorization_result->bytes, authorization_result->len, NULL, NULL, 0);
    }
}

static void s_aws_signing_complete(struct aws_signing_result *result, int error_code, void *userdata) {
    struct aws_dotnet_signing_callback_state *callback_state = userdata;

    if (result == NULL || error_code != AWS_ERROR_SUCCESS) {
        s_complete_signing_exceptionally(
            callback_state, (error_code != AWS_ERROR_SUCCESS) ? error_code : AWS_ERROR_UNKNOWN);
        goto done;
    }

    if (callback_state->request != NULL) {
        s_complete_http_request_signing(callback_state, result);
    } else {
        s_complete_signature_signing(callback_state, result);
    }

done:

    s_destroy_signing_callback_state(callback_state);
}

AWS_DOTNET_API void aws_dotnet_auth_sign_http_request(
    const char *method,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    struct aws_dotnet_stream_function_table body_stream_delegates,
    struct aws_signing_config_native native_signing_config,
    uint64_t callback_id,
    aws_dotnet_auth_on_signing_complete_fn *on_signing_complete) {

    int32_t error_code = AWS_ERROR_SUCCESS;
    struct aws_dotnet_signing_callback_state *continuation = NULL;

    struct aws_signing_config_aws config;
    AWS_ZERO_STRUCT(config);

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    continuation = aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_signing_callback_state));
    if (continuation == NULL) {
        goto on_error;
    }

    if (s_initialize_signing_config(&config, &native_signing_config, continuation)) {
        goto on_error;
    }

    continuation->callback_id = callback_id;
    continuation->on_signing_complete = on_signing_complete;
    continuation->signature_type = native_signing_config.signature_type;
    continuation->should_sign_header = native_signing_config.should_sign_header;
    continuation->request = aws_build_http_request(method, uri, headers, header_count, &body_stream_delegates);
    if (continuation->request == NULL) {
        goto on_error;
    }

    continuation->original_request_signable = aws_signable_new_http_request(allocator, continuation->request);
    if (continuation->original_request_signable == NULL) {
        goto on_error;
    }

    /* Sign the native request */
    if (aws_sign_request_aws(
            allocator,
            continuation->original_request_signable,
            (struct aws_signing_config_base *)&config,
            s_aws_signing_complete,
            continuation)) {
        goto on_error;
    }

    return;

on_error:

    s_destroy_signing_callback_state(continuation);

    error_code = aws_last_error();
    if (error_code == AWS_ERROR_SUCCESS) {
        error_code = AWS_ERROR_UNKNOWN;
    }

    on_signing_complete(callback_id, error_code, NULL, 0, NULL, NULL, 0);
}

AWS_DOTNET_API void aws_dotnet_auth_sign_canonical_request(
    const char *canonical_request,
    struct aws_signing_config_native native_signing_config,
    uint64_t callback_id,
    aws_dotnet_auth_on_signing_complete_fn *on_signing_complete) {

    int32_t error_code = AWS_ERROR_SUCCESS;
    struct aws_dotnet_signing_callback_state *continuation = NULL;

    struct aws_signing_config_aws config;
    AWS_ZERO_STRUCT(config);

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    continuation = aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_signing_callback_state));
    if (continuation == NULL) {
        goto on_error;
    }

    if (s_initialize_signing_config(&config, &native_signing_config, continuation)) {
        goto on_error;
    }

    continuation->callback_id = callback_id;
    continuation->on_signing_complete = on_signing_complete;
    continuation->signature_type = native_signing_config.signature_type;

    struct aws_byte_cursor canonical_request_cursor = aws_byte_cursor_from_c_str(canonical_request);
    continuation->original_request_signable = aws_signable_new_canonical_request(allocator, canonical_request_cursor);
    if (continuation->original_request_signable == NULL) {
        goto on_error;
    }

    /* Sign the native request */
    if (aws_sign_request_aws(
            allocator,
            continuation->original_request_signable,
            (struct aws_signing_config_base *)&config,
            s_aws_signing_complete,
            continuation)) {
        goto on_error;
    }

    return;

on_error:

    s_destroy_signing_callback_state(continuation);

    error_code = aws_last_error();
    if (error_code == AWS_ERROR_SUCCESS) {
        error_code = AWS_ERROR_UNKNOWN;
    }

    on_signing_complete(callback_id, error_code, NULL, 0, NULL, NULL, 0);
}

AWS_DOTNET_API void aws_dotnet_auth_sign_chunk(
    struct aws_dotnet_stream_function_table chunk_body_stream_delegates,
    uint8_t *previous_signature,
    uint32_t previous_signature_size,
    struct aws_signing_config_native native_signing_config,
    uint64_t callback_id,
    aws_dotnet_auth_on_signing_complete_fn *on_signing_complete) {

    int32_t error_code = AWS_ERROR_SUCCESS;
    struct aws_dotnet_signing_callback_state *continuation = NULL;

    struct aws_signing_config_aws config;
    AWS_ZERO_STRUCT(config);

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    continuation = aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_signing_callback_state));
    if (continuation == NULL) {
        goto on_error;
    }

    if (s_initialize_signing_config(&config, &native_signing_config, continuation)) {
        goto on_error;
    }

    continuation->callback_id = callback_id;
    continuation->on_signing_complete = on_signing_complete;
    continuation->signature_type = native_signing_config.signature_type;

    struct aws_byte_cursor previous_signature_cursor;
    AWS_ZERO_STRUCT(previous_signature_cursor);
    previous_signature_cursor.ptr = previous_signature;
    previous_signature_cursor.len = previous_signature_size;

    if (aws_stream_function_table_is_valid(&chunk_body_stream_delegates)) {
        continuation->body_stream = aws_input_stream_new_dotnet(allocator, &chunk_body_stream_delegates);
        if (continuation->body_stream == NULL) {
            goto on_error;
        }
    }

    continuation->original_request_signable =
        aws_signable_new_chunk(allocator, continuation->body_stream, previous_signature_cursor);
    if (continuation->original_request_signable == NULL) {
        goto on_error;
    }

    /* Sign the native request */
    if (aws_sign_request_aws(
            allocator,
            continuation->original_request_signable,
            (struct aws_signing_config_base *)&config,
            s_aws_signing_complete,
            continuation)) {
        goto on_error;
    }

    return;

on_error:

    s_destroy_signing_callback_state(continuation);

    error_code = aws_last_error();
    if (error_code == AWS_ERROR_SUCCESS) {
        error_code = AWS_ERROR_UNKNOWN;
    }

    on_signing_complete(callback_id, error_code, NULL, 0, NULL, NULL, 0);
}

AWS_DOTNET_API void aws_dotnet_auth_sign_trailing_headers(
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    uint8_t *previous_signature,
    uint32_t previous_signature_size,
    struct aws_signing_config_native native_signing_config,
    uint64_t callback_id,
    aws_dotnet_auth_on_signing_complete_fn *on_signing_complete) {

    int32_t error_code = AWS_ERROR_SUCCESS;
    struct aws_dotnet_signing_callback_state *continuation = NULL;

    struct aws_signing_config_aws config;
    AWS_ZERO_STRUCT(config);

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    continuation = aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_signing_callback_state));
    if (continuation == NULL) {
        goto on_error;
    }

    if (s_initialize_signing_config(&config, &native_signing_config, continuation)) {
        goto on_error;
    }

    continuation->callback_id = callback_id;
    continuation->on_signing_complete = on_signing_complete;
    continuation->signature_type = native_signing_config.signature_type;

    struct aws_byte_cursor previous_signature_cursor;
    AWS_ZERO_STRUCT(previous_signature_cursor);
    previous_signature_cursor.ptr = previous_signature;
    previous_signature_cursor.len = previous_signature_size;

    struct aws_http_headers *trailing_headers = aws_build_http_headers(headers, header_count);
    if (trailing_headers == NULL) {
        goto on_error;
    }

    continuation->original_request_signable =
        aws_signable_new_trailing_headers(allocator, trailing_headers, previous_signature_cursor);
    if (continuation->original_request_signable == NULL) {
        goto on_error;
    }

    /* Sign the native request */
    if (aws_sign_request_aws(
            allocator,
            continuation->original_request_signable,
            (struct aws_signing_config_base *)&config,
            s_aws_signing_complete,
            continuation)) {
        goto on_error;
    }

    return;

on_error:

    s_destroy_signing_callback_state(continuation);

    error_code = aws_last_error();
    if (error_code == AWS_ERROR_SUCCESS) {
        error_code = AWS_ERROR_UNKNOWN;
    }

    on_signing_complete(callback_id, error_code, NULL, 0, NULL, NULL, 0);
}

AWS_DOTNET_API bool aws_dotnet_auth_verify_v4a_canonical_signing(
    const char *canonical_request,
    struct aws_signing_config_native native_signing_config,
    const char *hex_signature,
    const char *ecc_pub_x,
    const char *ecc_pub_y) {

    (void)canonical_request;
    (void)native_signing_config;
    (void)hex_signature;
    (void)ecc_pub_x;
    (void)ecc_pub_y;

    struct aws_byte_cursor canonical_request_cursor = aws_byte_cursor_from_c_str(canonical_request);

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    int result = AWS_OP_ERR;
    struct aws_dotnet_signing_callback_state *continuation =
        aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_signing_callback_state));
    if (continuation == NULL) {
        return false;
    }

    struct aws_signing_config_aws config;
    AWS_ZERO_STRUCT(config);

    if (s_initialize_signing_config(&config, &native_signing_config, continuation)) {
        goto done;
    }

    struct aws_signable *signable = aws_signable_new_canonical_request(allocator, canonical_request_cursor);
    if (signable == NULL) {
        goto done;
    }

    result = aws_verify_sigv4a_signing(
        allocator,
        signable,
        (struct aws_signing_config_base *)&config,
        aws_byte_cursor_from_c_str(canonical_request),
        aws_byte_cursor_from_c_str(hex_signature),
        aws_byte_cursor_from_c_str(ecc_pub_x),
        aws_byte_cursor_from_c_str(ecc_pub_y));

done:

    s_destroy_signing_callback_state(continuation);

    return result == AWS_OP_SUCCESS;
}

AWS_DOTNET_API bool aws_dotnet_auth_verify_v4a_signature(
    const char *string_to_sign,
    uint8_t *signature,
    uint32_t signature_size,
    const char *ecc_pub_x,
    const char *ecc_pub_y) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    struct aws_ecc_key_pair *ecc_key = aws_ecc_key_new_from_hex_coordinates(
        allocator, AWS_CAL_ECDSA_P256, aws_byte_cursor_from_c_str(ecc_pub_x), aws_byte_cursor_from_c_str(ecc_pub_y));

    struct aws_byte_cursor signature_cursor = {
        .ptr = signature,
        .len = signature_size,
    };

    int result = aws_validate_v4a_authorization_value(
        allocator, ecc_key, aws_byte_cursor_from_c_str(string_to_sign), signature_cursor);

    aws_ecc_key_pair_release(ecc_key);

    return result == AWS_OP_SUCCESS;
}

AWS_DOTNET_API bool aws_dotnet_auth_verify_v4a_http_request_signature(
    const char *method,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    struct aws_dotnet_stream_function_table body_stream_delegates,
    const char *canonical_request,
    struct aws_signing_config_native native_signing_config,
    const char *hex_signature,
    const char *ecc_pub_x,
    const char *ecc_pub_y) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    int result = AWS_OP_ERR;
    struct aws_dotnet_signing_callback_state *continuation =
        aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_signing_callback_state));
    if (continuation == NULL) {
        return false;
    }

    struct aws_signing_config_aws config;
    AWS_ZERO_STRUCT(config);

    if (s_initialize_signing_config(&config, &native_signing_config, continuation)) {
        goto done;
    }

    continuation->signature_type = native_signing_config.signature_type;
    continuation->should_sign_header = native_signing_config.should_sign_header;
    continuation->request = aws_build_http_request(method, uri, headers, header_count, &body_stream_delegates);
    if (continuation->request == NULL) {
        goto done;
    }

    continuation->body_stream = aws_http_message_get_body_stream(continuation->request);
    continuation->original_request_signable = aws_signable_new_http_request(allocator, continuation->request);
    if (continuation->original_request_signable == NULL) {
        goto done;
    }

    result = aws_verify_sigv4a_signing(
        allocator,
        continuation->original_request_signable,
        (struct aws_signing_config_base *)&config,
        aws_byte_cursor_from_c_str(canonical_request),
        aws_byte_cursor_from_c_str(hex_signature),
        aws_byte_cursor_from_c_str(ecc_pub_x),
        aws_byte_cursor_from_c_str(ecc_pub_y));

done:

    s_destroy_signing_callback_state(continuation);

    return result == AWS_OP_SUCCESS;
}