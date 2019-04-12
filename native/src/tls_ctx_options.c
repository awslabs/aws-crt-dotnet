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

//__inline__ static void trap_instruction(void) {
//    __asm__ volatile("int $0x03");
//}

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

    //trap_instruction();
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_default_client(options, allocator);

    return options;
}

/*
AWS_DOTNET_API
struct aws_tls_ctx_options *aws_dotnet_tls_ctx_options_new_default_server() {
    struct aws_tls_ctx_options *options = s_tls_ctx_options_new();
    if (!options) {
        return NULL;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_ctx_options_init_default_server(options, allocator);

    return options;
}
*/

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
