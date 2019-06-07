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
