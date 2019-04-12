/*
 * Copyright 2010-2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
#include "crt.h"
#include "exports.h"

#include <aws/io/tls_channel_handler.h>

struct aws_tls_ctx_options *s_tls_ctx_options_new() {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_tls_ctx_options *options = aws_mem_acquire(allocator, sizeof(struct aws_tls_ctx_options));
    if (!options) {
        aws_dotnet_throw_exception("Failed to allocate new aws_tls_ctx_options");
        return NULL;
    }
    AWS_ZERO_STRUCT(*options);
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
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return NULL;
    }
    if (cert_path == NULL || key_path == NULL) {
        aws_dotnet_throw_exception("certPath and privateKeyPath must not be null");
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
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    options->minimum_tls_version = (enum aws_tls_versions)version;
}

AWS_DOTNET_API
int32_t aws_dotnet_tls_ctx_options_get_minimum_tls_version(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return -1;
    }
    return (int32_t)options->minimum_tls_version;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_set_alpn_list(struct aws_tls_ctx_options *options, const char *alpn) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    if (alpn == NULL) {
        aws_dotnet_throw_exception("Invalid alpn list: must not be null");
        return;
    }
    aws_tls_ctx_options_set_alpn_list(options, alpn);
}

AWS_DOTNET_API
const char *aws_dotnet_tls_ctx_options_get_alpn_list(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
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
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    options->max_fragment_size = (size_t)max_fragment_size;
}

AWS_DOTNET_API
intptr_t aws_dotnet_tls_ctx_options_get_max_fragment_size(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return 0;
    }
    return (intptr_t)options->max_fragment_size;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_set_verify_peer(struct aws_tls_ctx_options *options, bool verify) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    options->verify_peer = verify;
}

AWS_DOTNET_API
bool aws_dotnet_tls_ctx_options_get_verify_peer(struct aws_tls_ctx_options *options) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return false;
    }
    return options->verify_peer;
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_override_default_trust_store_from_path(struct aws_tls_ctx_options *options, const char* ca_path, const char* ca_file) {
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    aws_tls_ctx_options_override_default_trust_store_from_path(options, ca_path, ca_file);
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_client_mtls_from_path(
    struct aws_tls_ctx_options *options,
    const char *cert_path,
    const char *key_path) {
#if defined(__APPLE__)
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_client_mtls_from_path(options, allocator, cert_path, key_path);
#endif
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_default_server_from_path(
    struct aws_tls_ctx_options *options,
    const char *cert_path,
    const char *key_path) {
#if defined(__APPLE__)
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_default_server_from_path(options, allocator, cert_path, key_path);
#endif
}

AWS_DOTNET_API
void aws_dotnet_tls_ctx_options_init_client_mtls_pkcs12_from_path(struct aws_tls_ctx_options *options, const char *pkcs12_path, const char *pkcs12_password) {
#if defined(__APPLE__)
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
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
#if defined(__APPLE__)
    if (options == NULL) {
        aws_dotnet_throw_exception("Invalid TlsContextOptions");
        return;
    }
    struct aws_byte_cursor password = aws_byte_cursor_from_c_str(pkcs12_password);
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_server_pkcs12_from_path(options, allocator, pkcs12_path, &password);
#endif
}
