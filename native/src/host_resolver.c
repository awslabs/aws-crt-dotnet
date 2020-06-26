/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "crt.h"
#include "exports.h"

#include <aws/io/host_resolver.h>

AWS_DOTNET_API
struct aws_host_resolver *aws_dotnet_host_resolver_new_default(struct aws_event_loop_group *elg, int max_hosts) {
    if (!elg) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid EventLoopGroup");
        return NULL;
    }
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_host_resolver *resolver = aws_mem_calloc(allocator, 1, sizeof(struct aws_host_resolver));
    if (!resolver) {
        aws_dotnet_throw_exception(aws_last_error(), "Failed to allocate new aws_host_resolver");
        return NULL;
    }

    if (aws_host_resolver_init_default(resolver, allocator, max_hosts, elg)) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize default host resolver");
        aws_mem_release(allocator, resolver);
        return NULL;
    }
    return resolver;
}

AWS_DOTNET_API
void aws_dotnet_host_resolver_destroy(struct aws_host_resolver *resolver) {
    if (resolver == NULL) {
        return;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_host_resolver_clean_up(resolver);
    aws_mem_release(allocator, resolver);
}
