/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "stream.h"
#include "crt.h"
#include "exports.h"

#include <aws/io/stream.h>

struct aws_input_stream_dotnet_impl {
    struct aws_input_stream base;
    struct aws_allocator *allocator;

    struct aws_dotnet_stream_function_table delegates;
    enum aws_stream_state state;
};

static int s_aws_input_stream_dotnet_seek(
    struct aws_input_stream *stream,
    aws_off_t offset,
    enum aws_stream_seek_basis basis) {

    struct aws_input_stream_dotnet_impl *impl = AWS_CONTAINER_OF(stream, struct aws_input_stream_dotnet_impl, base);
    bool success = impl->delegates.seek((int64_t)offset, (int32_t)basis);

    return success ? AWS_OP_SUCCESS : AWS_OP_ERR;
}

static int s_aws_input_stream_dotnet_read(struct aws_input_stream *stream, struct aws_byte_buf *dest) {
    struct aws_input_stream_dotnet_impl *impl = AWS_CONTAINER_OF(stream, struct aws_input_stream_dotnet_impl, base);

    uint64_t buf_size = dest->capacity - dest->len;
    uint8_t *buf_ptr = dest->buffer + dest->len;
    uint64_t bytes_written = 0;
    impl->state = impl->delegates.read(buf_ptr, buf_size, &bytes_written);
    AWS_FATAL_ASSERT(bytes_written <= buf_size && "Buffer overflow detected streaming outgoing body");
    dest->len += (size_t)bytes_written;

    return AWS_OP_SUCCESS;
}

static int s_aws_input_stream_dotnet_get_status(struct aws_input_stream *stream, struct aws_stream_status *status) {
    struct aws_input_stream_dotnet_impl *impl = AWS_CONTAINER_OF(stream, struct aws_input_stream_dotnet_impl, base);

    status->is_end_of_stream = impl->state == STREAM_STATE_DONE;
    status->is_valid = true;

    return AWS_OP_SUCCESS;
}

static int s_aws_input_stream_dotnet_get_length(struct aws_input_stream *stream, int64_t *out_length) {
    (void)stream;
    (void)out_length;

    return AWS_OP_ERR;
}

static void s_aws_input_stream_dotnet_destroy(struct aws_input_stream_dotnet_impl *impl) {
    aws_mem_release(impl->allocator, impl);
}

static struct aws_input_stream_vtable s_aws_input_stream_dotnet_vtable = {
    .seek = s_aws_input_stream_dotnet_seek,
    .read = s_aws_input_stream_dotnet_read,
    .get_status = s_aws_input_stream_dotnet_get_status,
    .get_length = s_aws_input_stream_dotnet_get_length,
};

struct aws_input_stream *aws_input_stream_new_dotnet(
    struct aws_allocator *allocator,
    struct aws_dotnet_stream_function_table *function_table) {

    AWS_FATAL_ASSERT(aws_stream_function_table_is_valid(function_table));

    struct aws_input_stream_dotnet_impl *impl =
        aws_mem_calloc(allocator, 1, sizeof(struct aws_input_stream_dotnet_impl));

    impl->allocator = allocator;
    impl->base.vtable = &s_aws_input_stream_dotnet_vtable;

    impl->delegates = *function_table;
    impl->state = STREAM_STATE_IN_PROGRESS;

    aws_ref_count_init(
        &impl->base.ref_count, impl, (aws_simple_completion_callback *)s_aws_input_stream_dotnet_destroy);

    return &impl->base;
}

bool aws_stream_function_table_is_valid(struct aws_dotnet_stream_function_table *function_table) {
    if (function_table == NULL) {
        return false;
    }

    if (function_table->read == NULL) {
        return false;
    }

    if (function_table->seek == NULL) {
        return false;
    }

    return true;
}
