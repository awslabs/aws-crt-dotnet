/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
#include "crt.h"
#include "exports.h"

#include <aws/common/string.h>
#include <aws/io/tls_channel_handler.h>

bool s_tls_args_to_options(
    struct aws_tls_ctx_options *options,
    enum aws_tls_versions min_tls_version,
    const char *ca_file,
    const char *ca_path,
    const char *alpn_list,
    const char *cert_path,
    const char *key_path,
    const char *pkcs12_path,
    const char *pkcs12_password,
    uint32_t max_fragment_size,
    uint8_t verify_peer) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    AWS_ZERO_STRUCT(*options);
    aws_tls_ctx_options_init_default_client(options, allocator);
    if (ca_path || ca_file) {
        aws_tls_ctx_options_override_default_trust_store_from_path(options, ca_path, ca_file);
    }
    if (cert_path && key_path) {
        aws_tls_ctx_options_init_client_mtls_from_path(options, allocator, cert_path, key_path);
    }
    if (pkcs12_path && pkcs12_password) {
#if defined(__APPLE__)
        struct aws_byte_cursor password = aws_byte_cursor_from_c_str(pkcs12_password);
        aws_tls_ctx_options_init_client_mtls_pkcs12_from_path(options, allocator, pkcs12_path, &password);
#else
        aws_dotnet_throw_exception(AWS_ERROR_UNSUPPORTED_OPERATION, "PKCS12 is not supported on non-Apple platforms");
        return false;
#endif
    }
    if (alpn_list) {
        aws_tls_ctx_options_set_alpn_list(options, alpn_list);
    }
    options->minimum_tls_version = min_tls_version;
    options->max_fragment_size = max_fragment_size;
    options->verify_peer = verify_peer != 0;
    return true;
}

AWS_DOTNET_API struct aws_tls_ctx *aws_dotnet_tls_ctx_new_client(
    enum aws_tls_versions min_tls_version,
    const char *ca_file,
    const char *ca_path,
    const char *alpn_list,
    const char *cert_path,
    const char *key_path,
    const char *pkcs12_path,
    const char *pkcs12_password,
    uint32_t max_fragment_size,
    uint8_t verify_peer) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_tls_ctx_options options;
    if (!s_tls_args_to_options(
            &options,
            min_tls_version,
            ca_file,
            ca_path,
            alpn_list,
            cert_path,
            key_path,
            pkcs12_path,
            pkcs12_password,
            max_fragment_size,
            verify_peer)) {
        return NULL;
    }
    struct aws_tls_ctx *tls = aws_tls_client_ctx_new(allocator, &options);
    aws_tls_ctx_options_clean_up(&options);
    if (!tls) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to create aws_tls_context");
        return NULL;
    }
    return tls;
}

AWS_DOTNET_API
struct aws_tls_ctx *aws_dotnet_tls_ctx_new_server(
    enum aws_tls_versions min_tls_version,
    const char *ca_file,
    const char *ca_path,
    const char *alpn_list,
    const char *cert_path,
    const char *key_path,
    const char *pkcs12_path,
    const char *pkcs12_password,
    uint32_t max_fragment_size,
    uint8_t verify_peer) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_tls_ctx_options options;
    if (!s_tls_args_to_options(
            &options,
            min_tls_version,
            ca_file,
            ca_path,
            alpn_list,
            cert_path,
            key_path,
            pkcs12_path,
            pkcs12_password,
            max_fragment_size,
            verify_peer)) {
        return NULL;
    }
    struct aws_tls_ctx *tls = aws_tls_server_ctx_new(allocator, &options);
    if (!tls) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to create aws_tls_context");
        return NULL;
    }
    return tls;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_destroy(struct aws_tls_ctx *tls) {
    aws_tls_ctx_release(tls);
}

struct aws_tls_ctx_options *s_tls_ctx_options_new(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_tls_ctx_options *options = aws_mem_calloc(allocator, 1, sizeof(struct aws_tls_ctx_options));
    if (!options) {
        aws_dotnet_throw_exception(aws_last_error(), "Failed to allocate new aws_tls_ctx_options");
        return NULL;
    }

    return options;
}

AWS_DOTNET_API
struct aws_tls_ctx_options *aws_dotnet_tls_ctx_options_new_default_client(void) {
    struct aws_tls_ctx_options *options = s_tls_ctx_options_new();
    if (!options) {
        return NULL;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_default_client(options, allocator);

    return options;
}

AWS_DOTNET_API
struct aws_tls_ctx_options *aws_dotnet_tls_ctx_options_new_default_server(const char *cert_path, const char *key_path) {
    struct aws_tls_ctx_options *options = s_tls_ctx_options_new();
    if (!options) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return NULL;
    }
    if (cert_path == NULL || key_path == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "certPath and privateKeyPath must not be null");
        return NULL;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_default_server_from_path(options, allocator, cert_path, key_path);

    return options;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_destroy(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        return;
    }

    aws_tls_ctx_options_clean_up(options);

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_mem_release(allocator, options);
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_set_minimum_tls_version(struct aws_tls_ctx_options *options, int32_t version) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    options->minimum_tls_version = (enum aws_tls_versions)version;
}

AWS_DOTNET_API
int32_t aws_dotnet_tls_ctx_options_get_minimum_tls_version(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return -1;
    }
    return (int32_t)options->minimum_tls_version;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_set_alpn_list(struct aws_tls_ctx_options *options, const char *alpn) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    if (alpn == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid alpn list: must not be null");
        return;
    }
    aws_tls_ctx_options_set_alpn_list(options, alpn);
}

AWS_DOTNET_API
const char *aws_dotnet_tls_ctx_options_get_alpn_list(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return "";
    }
    if (!options->alpn_list) {
        return "";
    }
    return (const char *)options->alpn_list->bytes;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_set_max_fragment_size(struct aws_tls_ctx_options *options, intptr_t max_fragment_size) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    options->max_fragment_size = (size_t)max_fragment_size;
}

AWS_DOTNET_API
intptr_t aws_dotnet_tls_ctx_options_get_max_fragment_size(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return 0;
    }
    return (intptr_t)options->max_fragment_size;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_set_verify_peer(struct aws_tls_ctx_options *options, bool verify) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    options->verify_peer = verify;
}

AWS_DOTNET_API
bool aws_dotnet_tls_ctx_options_get_verify_peer(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return false;
    }
    return options->verify_peer;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_override_default_trust_store_from_path(
    struct aws_tls_ctx_options *options,
    const char *ca_path,
    const char *ca_file) {
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    aws_tls_ctx_options_override_default_trust_store_from_path(options, ca_path, ca_file);
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_client_mtls_from_path(
    struct aws_tls_ctx_options *options,
    const char *cert_path,
    const char *key_path) {

    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_client_mtls_from_path(options, allocator, cert_path, key_path);
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_default_server_from_path(
    struct aws_tls_ctx_options *options,
    const char *cert_path,
    const char *key_path) {

    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_default_server_from_path(options, allocator, cert_path, key_path);
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_client_mtls_pkcs12_from_path(
    struct aws_tls_ctx_options *options,
    const char *pkcs12_path,
    const char *pkcs12_password) {

    (void)options;
    (void)pkcs12_path;
    (void)pkcs12_password;
#if defined(__APPLE__)
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    struct aws_byte_cursor password = aws_byte_cursor_from_c_str(pkcs12_password);
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_client_mtls_pkcs12_from_path(options, allocator, pkcs12_path, &password);
#endif
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_server_pkcs12_from_path(
    struct aws_tls_ctx_options *options,
    const char *pkcs12_path,
    const char *pkcs12_password) {

    (void)options;
    (void)pkcs12_path;
    (void)pkcs12_password;
#if defined(__APPLE__)
    if (options == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid TlsContextOptions");
        return;
    }
    struct aws_byte_cursor password = aws_byte_cursor_from_c_str(pkcs12_password);
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_server_pkcs12_from_path(options, allocator, pkcs12_path, &password);
#endif
}

AWS_DOTNET_API
struct aws_tls_connection_options *aws_dotnet_tls_connection_options_new(
    struct aws_tls_ctx *tls_ctx,
    const char *server_name,
    const char *alpn_list) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_tls_connection_options *options =
        aws_mem_calloc(allocator, 1, sizeof(struct aws_tls_connection_options));
    if (!options) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to allocate aws_tls_connection_options");
        return NULL;
    }
    aws_tls_connection_options_init_from_ctx(options, tls_ctx);
    if (server_name) {
        struct aws_byte_cursor server = aws_byte_cursor_from_c_str(server_name);
        if (aws_tls_connection_options_set_server_name(options, allocator, &server)) {
            goto error;
        }
    }
    if (alpn_list && aws_tls_connection_options_set_alpn_list(options, allocator, alpn_list)) {
        goto error;
    }

    return options;

error:
    if (options) {
        aws_mem_release(allocator, options);
    }
    return NULL;
}

AWS_DOTNET_API
void aws_dotnet_tls_connection_options_destroy(struct aws_tls_connection_options *options) {
    if (!options) {
        return;
    }
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_connection_options_clean_up(options);
    aws_mem_release(allocator, options);
}
