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

typedef void(aws_dotnet_auth_on_signing_complete_fn)(
    uint64_t callback_id,
    int32_t error_code,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count);

struct aws_dotnet_signing_callback_state {
    struct aws_http_message *request;
    struct aws_signable *original_request_signable;
    struct aws_credentials *credentials;
    struct aws_string *region;
    struct aws_string *service;
    struct aws_string *signed_body_value;
    aws_dotnet_auth_should_sign_header_fn *should_sign_header;
    uint64_t callback_id;
    aws_dotnet_auth_on_signing_complete_fn *on_signing_complete;
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

    if (callback_state->request != NULL) {
        struct aws_input_stream *body_stream = aws_http_message_get_body_stream(callback_state->request);
        if (body_stream != NULL) {
            aws_input_stream_destroy(body_stream);
        }

        aws_http_message_release(callback_state->request);
    }

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
    callback_state->on_signing_complete(callback_state->callback_id, error_code, NULL, NULL, 0);
}

static void s_complete_signing_normally(struct aws_dotnet_signing_callback_state *callback_state) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    struct aws_byte_cursor path_cursor;
    AWS_ZERO_STRUCT(path_cursor);

    aws_http_message_get_request_path(callback_state->request, &path_cursor);
    struct aws_string *uri = aws_string_new_from_cursor(allocator, &path_cursor);

    size_t header_count = aws_http_message_get_header_count(callback_state->request);
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_dotnet_http_header, dotnet_headers, header_count);
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_string *, header_strings, header_count * 2);

    for (size_t header_idx = 0; header_idx < header_count; ++header_idx) {
        size_t string_index = header_idx * 2;
        header_strings[string_index] = NULL;
        header_strings[string_index + 1] = NULL;

        AWS_ZERO_STRUCT(dotnet_headers[header_idx]);

        struct aws_http_header header;
        AWS_ZERO_STRUCT(header);

        if (aws_http_message_get_header(callback_state->request, &header, header_idx)) {
            continue;
        }

        header_strings[string_index] = aws_string_new_from_array(allocator, header.name.ptr, header.name.len);
        header_strings[string_index + 1] = aws_string_new_from_array(allocator, header.value.ptr, header.value.len);

        dotnet_headers[header_idx].name = (const char *)header_strings[string_index]->bytes;
        dotnet_headers[header_idx].value = (const char *)header_strings[string_index + 1]->bytes;
    }

    callback_state->on_signing_complete(
        callback_state->callback_id,
        AWS_ERROR_SUCCESS,
        (const char *)uri->bytes,
        dotnet_headers,
        (uint32_t)header_count);

    aws_string_destroy(uri);

    for (size_t idx = 0; idx < header_count * 2; ++idx) {
        aws_string_destroy(header_strings[idx]);
    }
}

static void s_aws_signing_complete(struct aws_signing_result *result, int error_code, void *userdata) {

    struct aws_dotnet_signing_callback_state *callback_state = userdata;
    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    if (result == NULL || error_code != AWS_ERROR_SUCCESS) {
        s_complete_signing_exceptionally(
            callback_state, (error_code != AWS_ERROR_SUCCESS) ? error_code : AWS_ERROR_UNKNOWN);
        goto done;
    }

    if (aws_apply_signing_result_to_http_request(callback_state->request, allocator, result)) {
        s_complete_signing_exceptionally(callback_state, aws_last_error());
        goto done;
    }

    s_complete_signing_normally(callback_state);

done:

    s_destroy_signing_callback_state(callback_state);
}

AWS_DOTNET_API void aws_dotnet_auth_sign_request(
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

    on_signing_complete(callback_id, error_code, NULL, NULL, 0);
}
