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

#include <aws/io/channel_bootstrap.h>

AWS_DOTNET_API
struct aws_client_bootstrap *aws_dotnet_client_bootstrap_new(struct aws_event_loop_group *elg) {
    if (elg == NULL) {
        aws_dotnet_throw_exception("Invalid EventLoopGroup");
        return NULL;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_client_bootstrap *bootstrap = aws_client_bootstrap_new(allocator, elg, NULL, NULL);
    if (!bootstrap) {
        aws_dotnet_throw_exception("Failed to allocate new aws_client_bootstrap");
        goto error;
    }

    return bootstrap;

error:
    if (bootstrap) {
        aws_client_bootstrap_destroy(bootstrap);
    }
    return NULL;
}

AWS_DOTNET_API
void aws_dotnet_client_bootstrap_destroy(struct aws_client_bootstrap *bootstrap) {
    if (bootstrap == NULL) {
        aws_dotnet_throw_exception("Invalid ClientBootstrap");
        return;
    }

    aws_client_bootstrap_destroy(bootstrap);
}
