/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#ifndef AWS_DOTNET_HTTP_CLIENT_H
#define AWS_DOTNET_HTTP_CLIENT_H

#include <aws/common/common.h>

struct aws_http_message;
struct aws_dotnet_stream_function_table;

struct aws_dotnet_http_header {
    const char *name;
    const char *value;
    int32_t name_size;
    int32_t value_size;
};

struct aws_http_message *aws_build_http_request(
    const char *method,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    struct aws_dotnet_stream_function_table *body_stream_delegates);

struct aws_http_headers *aws_build_http_headers(struct aws_dotnet_http_header headers[], uint32_t header_count);

#endif /* AWS_DOTNET_HTTP_CLIENT_H */
