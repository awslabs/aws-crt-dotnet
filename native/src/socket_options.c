/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "crt.h"
#include "exports.h"

#include <aws/io/socket.h>

AWS_DOTNET_API struct aws_socket_options *aws_dotnet_socket_options_new(
    enum aws_socket_type type,
    enum aws_socket_domain domain,
    uint32_t connect_timeout_ms,
    uint16_t keep_alive_interval_sec,
    uint16_t keep_alive_timeout_sec,
    uint16_t keep_alive_max_failed_probes,
    bool keepalive) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_socket_options *options = aws_mem_calloc(allocator, 1, sizeof(struct aws_socket_options));
    if (!options) {
        aws_dotnet_throw_exception(aws_last_error(), "Failed to allocate new aws_socket_options");
        return NULL;
    }

    options->type = type;
    options->domain = domain;
    options->connect_timeout_ms = connect_timeout_ms;
    options->keep_alive_interval_sec = keep_alive_interval_sec;
    options->keep_alive_timeout_sec = keep_alive_timeout_sec;
    options->keep_alive_max_failed_probes = keep_alive_max_failed_probes;
    options->keepalive = keepalive;

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
