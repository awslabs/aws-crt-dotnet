/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "crt.h"
#include "exports.h"

#include <aws/io/channel_bootstrap.h>

#if defined(_MSC_VER)
#    pragma warning(disable : 4204)
#endif /* _MSC_VER */

AWS_DOTNET_API
struct aws_client_bootstrap *aws_dotnet_client_bootstrap_new(
    struct aws_event_loop_group *elg,
    struct aws_host_resolver *host_resolver) {
    if (elg == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid EventLoopGroup");
        return NULL;
    }
    if (host_resolver == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid HostResolver");
        return NULL;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_client_bootstrap_options options = {
        .event_loop_group = elg,
        .host_resolver = host_resolver,
    };
    struct aws_client_bootstrap *bootstrap = aws_client_bootstrap_new(allocator, &options);
    if (!bootstrap) {
        aws_dotnet_throw_exception(aws_last_error(), "Failed to allocate new aws_client_bootstrap");
        return NULL;
    }

    return bootstrap;
}

AWS_DOTNET_API
void aws_dotnet_client_bootstrap_destroy(struct aws_client_bootstrap *bootstrap) {
    if (bootstrap == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid ClientBootstrap");
        return;
    }

    aws_client_bootstrap_release(bootstrap);
}
