/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#ifndef AWS_DOTNET_STREAM_H
#define AWS_DOTNET_STREAM_H

#include <aws/common/common.h>

#include "crt.h"

struct aws_input_stream;

enum aws_stream_state {
    STREAM_STATE_IN_PROGRESS,
    STREAM_STATE_DONE,
};

typedef int(DOTNET_CALL aws_dotnet_stream_read_fn)(uint8_t *buffer, uint64_t buffer_size, uint64_t *bytes_written);
typedef bool(DOTNET_CALL aws_dotnet_stream_seek_fn)(int64_t offset, int32_t basis);

struct aws_dotnet_stream_function_table {
    aws_dotnet_stream_read_fn *read;
    aws_dotnet_stream_seek_fn *seek;
};

struct aws_input_stream *aws_input_stream_new_dotnet(
    struct aws_allocator *allocator,
    struct aws_dotnet_stream_function_table *function_table);

bool aws_stream_function_table_is_valid(struct aws_dotnet_stream_function_table *function_table);

#endif /* AWS_DOTNET_STREAM_H */
