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

#include <aws/io/socket.h>

AWS_DOTNET_API
struct aws_socket_options *aws_dotnet_socket_options_new() {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_socket_options *options = aws_mem_acquire(allocator, sizeof(struct aws_socket_options));
    if (!options) {
        aws_dotnet_throw_exception("Failed to allocate new aws_socket_options");
        return NULL;
    }
    AWS_ZERO_STRUCT(*options);
    options->connect_timeout_ms = 3000;
    options->type = AWS_SOCKET_STREAM;

    return options;
}

AWS_DOTNET_API
void aws_dotnet_socket_options_destroy(struct aws_socket_options *options) {
    if (options == NULL) {
        return;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_mem_release(allocator, options);
}
