/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
#include "crt.h"
#include "exports.h"
#include "http_client.h"

#include <aws/auth/signing_config.h>

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

    int32_t Signed_body_header;

    uint64_t expiration_in_seconds;
};

AWS_DOTNET_API void aws_dotnet_auth_sign_request(
    const char *method,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    struct aws_signing_config_native native_signing_config) {
        
    (void)method;
    (void)uri;
    (void)headers;
    (void)header_count;
    (void)native_signing_config;
}
