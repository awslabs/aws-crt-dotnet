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

#include <aws/common/string.h>
#include <aws/http/connection.h>
#include <aws/http/request_response.h>
#include <aws/io/socket.h>
#include <aws/io/stream.h>

typedef void(DOTNET_CALL aws_dotnet_http_on_client_connection_setup_fn)(int error_code);

typedef void(DOTNET_CALL aws_dotnet_http_on_client_connection_shutdown_fn)(int error_code);

struct aws_dotnet_http_connection {
    struct aws_http_connection *connection;
    aws_dotnet_http_on_client_connection_setup_fn *on_setup;
    aws_dotnet_http_on_client_connection_shutdown_fn *on_shutdown;
};

static void s_http_connection_on_setup(struct aws_http_connection *connection, int error_code, void *user_data) {
    (void)connection;
    struct aws_dotnet_http_connection *dotnet_connection = user_data;
    dotnet_connection->connection = connection;
    dotnet_connection->on_setup(error_code);
}

static void s_http_connection_on_shutdown(struct aws_http_connection *connection, int error_code, void *user_data) {
    (void)connection;
    struct aws_dotnet_http_connection *dotnet_connection = user_data;
    dotnet_connection->on_shutdown(error_code);
    dotnet_connection->connection = NULL;
}

static struct aws_socket_options s_default_socket_options = {.type = AWS_SOCKET_STREAM,
                                                             .domain = AWS_SOCKET_IPV4,
                                                             .connect_timeout_ms = 3000};

AWS_DOTNET_API
struct aws_dotnet_http_connection *aws_dotnet_http_connection_new(
    struct aws_client_bootstrap *client_bootstrap,
    uint64_t initial_window_size,
    const char *host_name,
    uint16_t port,
    struct aws_socket_options *socket_options,
    struct aws_tls_connection_options *tls_connection_options,
    aws_dotnet_http_on_client_connection_setup_fn *on_setup,
    aws_dotnet_http_on_client_connection_shutdown_fn *on_shutdown) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_dotnet_http_connection *connection =
        aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_http_connection));
    if (!connection) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to allocate new aws_dotnet_http_connection");
        return NULL;
    }

    struct aws_http_client_connection_options options = AWS_HTTP_CLIENT_CONNECTION_OPTIONS_INIT;
    options.allocator = allocator;
    options.bootstrap = client_bootstrap;
    if (initial_window_size != 0) {
        options.initial_window_size = (size_t)initial_window_size;
    }
    options.host_name = aws_byte_cursor_from_c_str(host_name);
    options.port = port;
    if (!socket_options) {
        socket_options = &s_default_socket_options;
    }
    options.socket_options = socket_options;
    options.tls_options = tls_connection_options;
    options.on_setup = s_http_connection_on_setup;
    options.on_shutdown = s_http_connection_on_shutdown;
    options.user_data = connection;

    connection->on_setup = on_setup;
    connection->on_shutdown = on_shutdown;

    if (aws_http_client_connect(&options)) {
        aws_mem_release(allocator, connection);
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize new HTTP client connection");
        return NULL;
    }
    return connection;
}

AWS_DOTNET_API
void aws_dotnet_http_connection_destroy(struct aws_dotnet_http_connection *connection) {
    aws_http_connection_close(connection->connection);
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_mem_release(allocator, connection);
}

struct aws_dotnet_http_header {
    const char *name;
    const char *value;
};

typedef int(aws_dotnet_http_stream_outgoing_body_fn)(uint8_t *buffer, uint64_t buffer_size, uint64_t *bytes_written);
typedef void(aws_dotnet_http_on_incoming_headers_fn)(
    int32_t status_code,
    int32_t header_block,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count);
typedef void(aws_dotnet_http_on_incoming_header_block_done_fn)(bool has_body);
typedef void(aws_dotnet_http_on_incoming_body_fn)(uint8_t *data, uint64_t size);
typedef void(aws_dotnet_http_on_stream_complete_fn)(int error_code);

struct aws_dotnet_http_stream {
    struct aws_dotnet_http_connection *connection;
    struct aws_http_stream *stream;
    struct aws_http_message *request;
    aws_dotnet_http_stream_outgoing_body_fn *stream_outgoing_body;
    aws_dotnet_http_on_incoming_headers_fn *on_incoming_headers;
    aws_dotnet_http_on_incoming_header_block_done_fn *on_incoming_headers_block_done;
    aws_dotnet_http_on_incoming_body_fn *on_incoming_body;
    aws_dotnet_http_on_stream_complete_fn *on_stream_complete;
};

enum aws_http_outgoing_body_state {
    HOBS_IN_PROGRESS,
    HOBS_DONE,
};

static enum aws_http_outgoing_body_state s_stream_stream_outgoing_body(
    struct aws_http_stream *s,
    struct aws_byte_buf *buf,
    void *user_data) {
    (void)s;
    struct aws_dotnet_http_stream *stream = user_data;
    uint64_t buf_size = buf->capacity - buf->len;
    uint8_t *buf_ptr = buf->buffer + buf->len;
    uint64_t bytes_written = 0;
    enum aws_http_outgoing_body_state state = stream->stream_outgoing_body(buf_ptr, buf_size, &bytes_written);
    AWS_FATAL_ASSERT(bytes_written <= buf_size && "Buffer overflow detected streaming outgoing body");
    buf->len += (size_t)bytes_written;
    return state;
}

static int s_stream_on_incoming_headers(
    struct aws_http_stream *s,
    enum aws_http_header_block header_block,
    const struct aws_http_header *headers,
    size_t header_count,
    void *user_data) {
    (void)s;

    struct aws_dotnet_http_stream *stream = user_data;
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_dotnet_http_header, dotnet_headers, header_count);
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_string *, header_strings, header_count * 2);
    for (size_t header_idx = 0; header_idx < header_count; ++header_idx) {
        header_strings[header_idx] =
            aws_string_new_from_array(allocator, headers[header_idx].name.ptr, headers[header_idx].name.len);
        header_strings[header_idx + 1] =
            aws_string_new_from_array(allocator, headers[header_idx].value.ptr, headers[header_idx].value.len);
        dotnet_headers[header_idx].name = (const char *)header_strings[header_idx]->bytes;
        dotnet_headers[header_idx].value = (const char *)header_strings[header_idx + 1]->bytes;
    }

    int status = 0;
    aws_http_stream_get_incoming_response_status(stream->stream, &status);
    stream->on_incoming_headers(status, header_block, dotnet_headers, (uint32_t)header_count);
    for (size_t idx = 0; idx < header_count * 2; ++idx) {
        aws_string_destroy(header_strings[idx]);
    }

    return AWS_OP_SUCCESS;
}

static int s_stream_on_incoming_header_block_done(
    struct aws_http_stream *s,
    enum aws_http_header_block header_block,
    void *user_data) {
    (void)s;
    struct aws_dotnet_http_stream *stream = user_data;
    if (stream->on_incoming_headers_block_done) {
        stream->on_incoming_headers_block_done(header_block);
    }

    return AWS_OP_SUCCESS;
}

static int s_stream_on_incoming_body(struct aws_http_stream *s, const struct aws_byte_cursor *data, void *user_data) {
    (void)s;
    struct aws_dotnet_http_stream *stream = user_data;
    if (stream->on_incoming_body) {
        stream->on_incoming_body(data->ptr, data->len);
    }

    return AWS_OP_SUCCESS;
}

static void s_stream_on_stream_complete(struct aws_http_stream *s, int error_code, void *user_data) {
    (void)s;
    struct aws_dotnet_http_stream *stream = user_data;
    stream->on_stream_complete(error_code);
}

/*
 * Temporary input stream implementation until we bind a http body stream wrapper.
 * No support for seek or true status.
 */
struct aws_input_stream_dotnet_impl {
    struct aws_dotnet_http_stream *stream_wrapper;
    enum aws_http_outgoing_body_state state;
};

static int s_aws_input_stream_dotnet_seek(
    struct aws_input_stream *stream,
    aws_off_t offset,
    enum aws_stream_seek_basis basis) {
    (void)stream;
    (void)offset;
    (void)basis;

    return AWS_OP_ERR;
}

static int s_aws_input_stream_dotnet_read(struct aws_input_stream *stream, struct aws_byte_buf *dest) {
    struct aws_input_stream_dotnet_impl *impl = stream->impl;

    impl->state = s_stream_stream_outgoing_body(impl->stream_wrapper->stream, dest, impl->stream_wrapper);

    return AWS_OP_SUCCESS;
}

static int s_aws_input_stream_dotnet_get_status(struct aws_input_stream *stream, struct aws_stream_status *status) {
    struct aws_input_stream_dotnet_impl *impl = stream->impl;

    status->is_end_of_stream = impl->state == HOBS_DONE;
    status->is_valid = true;

    return AWS_OP_SUCCESS;
}

static int s_aws_input_stream_dotnet_get_length(struct aws_input_stream *stream, int64_t *out_length) {
    (void)stream;
    (void)out_length;

    return AWS_OP_ERR;
}

static void s_aws_input_stream_dotnet_destroy(struct aws_input_stream *stream) {
    aws_mem_release(stream->allocator, stream);
}

static struct aws_input_stream_vtable s_aws_input_stream_dotnet_vtable = {
    .seek = s_aws_input_stream_dotnet_seek,
    .read = s_aws_input_stream_dotnet_read,
    .get_status = s_aws_input_stream_dotnet_get_status,
    .get_length = s_aws_input_stream_dotnet_get_length,
    .destroy = s_aws_input_stream_dotnet_destroy};

static struct aws_input_stream *s_aws_input_stream_new_dotnet(
    struct aws_allocator *allocator,
    struct aws_dotnet_http_stream *stream_wrapper) {

    struct aws_input_stream *input_stream = NULL;
    struct aws_input_stream_dotnet_impl *impl = NULL;

    aws_mem_acquire_many(
        allocator,
        2,
        &input_stream,
        sizeof(struct aws_input_stream),
        &impl,
        sizeof(struct aws_input_stream_dotnet_impl));

    if (!input_stream) {
        return NULL;
    }

    AWS_ZERO_STRUCT(*input_stream);
    AWS_ZERO_STRUCT(*impl);

    input_stream->allocator = allocator;
    input_stream->vtable = &s_aws_input_stream_dotnet_vtable;
    input_stream->impl = impl;

    impl->stream_wrapper = stream_wrapper;
    impl->state = HOBS_IN_PROGRESS;

    return input_stream;
}

static struct aws_http_message *s_create_http_request(
    const char *method,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    struct aws_dotnet_http_stream *stream_wrapper) {

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_http_header, req_headers, (header_count ? header_count : 1));
    struct aws_http_message *request = aws_http_message_new_request(allocator);
    if (request == NULL) {
        return NULL;
    }

    if (aws_http_message_set_request_method(request, aws_byte_cursor_from_c_str(method))) {
        goto on_error;
    }

    if (aws_http_message_set_request_path(request, aws_byte_cursor_from_c_str(uri))) {
        goto on_error;
    }

    if (header_count > 0) {
        for (size_t header_idx = 0; header_idx < header_count; ++header_idx) {
            req_headers[header_idx].name = aws_byte_cursor_from_c_str(headers[header_idx].name);
            req_headers[header_idx].value = aws_byte_cursor_from_c_str(headers[header_idx].value);
        }

        if (aws_http_message_add_header_array(request, req_headers, header_count)) {
            goto on_error;
        }
    }

    if (stream_wrapper->stream_outgoing_body != NULL) {
        struct aws_input_stream *body_stream = s_aws_input_stream_new_dotnet(allocator, stream_wrapper);
        if (body_stream == NULL) {
            goto on_error;
        }

        aws_http_message_set_body_stream(request, body_stream);
    }

    return request;

on_error:

    aws_http_message_release(request);

    return NULL;
}

static void s_destroy_stream_wrapper(struct aws_dotnet_http_stream *stream_wrapper) {
    if (stream_wrapper == NULL) {
        return;
    }

    if (stream_wrapper->request != NULL) {
        struct aws_input_stream *body_stream = aws_http_message_get_body_stream(stream_wrapper->request);
        if (body_stream != NULL) {
            aws_input_stream_destroy(body_stream);
        }

        aws_http_message_release(stream_wrapper->request);
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_http_stream_release(stream_wrapper->stream);
    aws_mem_release(allocator, stream_wrapper);
}

AWS_DOTNET_API struct aws_dotnet_http_stream *aws_dotnet_http_stream_new(
    struct aws_dotnet_http_connection *connection,
    const char *method,
    const char *uri,
    struct aws_dotnet_http_header headers[],
    uint32_t header_count,
    aws_dotnet_http_stream_outgoing_body_fn *stream_outgoing_body,
    aws_dotnet_http_on_incoming_headers_fn *on_incoming_headers,
    aws_dotnet_http_on_incoming_header_block_done_fn *on_incoming_headers_block_done,
    aws_dotnet_http_on_incoming_body_fn *on_incoming_body,
    aws_dotnet_http_on_stream_complete_fn *on_stream_complete) {

    if (on_incoming_headers == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "on_incoming_headers must be provided");
        return NULL;
    }

    if (on_stream_complete == NULL) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "on_stream_complete must be provided");
        return NULL;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    struct aws_dotnet_http_stream *stream = aws_mem_calloc(allocator, 1, sizeof(struct aws_dotnet_http_stream));
    if (!stream) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to allocate aws_dotnet_http_stream");
        return NULL;
    }

    stream->connection = connection;
    stream->stream_outgoing_body = stream_outgoing_body;
    stream->on_incoming_headers = on_incoming_headers;
    stream->on_incoming_headers_block_done = on_incoming_headers_block_done;
    stream->on_incoming_body = on_incoming_body;
    stream->on_stream_complete = on_stream_complete;

    stream->request = s_create_http_request(method, uri, headers, header_count, stream);
    if (stream->request == NULL) {
        goto on_error;
    }

    struct aws_http_make_request_options options;
    AWS_ZERO_STRUCT(options);
    options.self_size = sizeof(struct aws_http_make_request_options);
    options.request = stream->request;
    options.on_response_headers = s_stream_on_incoming_headers;
    options.on_response_header_block_done = s_stream_on_incoming_header_block_done;
    options.on_response_body = s_stream_on_incoming_body;
    options.on_complete = s_stream_on_stream_complete;
    options.user_data = stream;

    stream->stream = aws_http_connection_make_request(connection->connection, &options);
    if (!stream->stream) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize new aws_http_stream");
        goto on_error;
    }

    return stream;

on_error:

    s_destroy_stream_wrapper(stream);

    return NULL;
}

AWS_DOTNET_API void aws_dotnet_http_stream_destroy(struct aws_dotnet_http_stream *stream) {
    s_destroy_stream_wrapper(stream);
}

AWS_DOTNET_API void aws_dotnet_http_stream_update_window(
    struct aws_dotnet_http_stream *stream,
    uint64_t increment_size) {
    if (!stream) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid HttpStream");
        return;
    }

    aws_http_stream_update_window(stream->stream, (size_t)increment_size);
}

AWS_DOTNET_API void aws_dotnet_http_stream_activate(struct aws_dotnet_http_stream *stream) {
    if (!stream) {
        aws_dotnet_throw_exception(AWS_ERROR_INVALID_ARGUMENT, "Invalid HttpStream");
        return;
    }

    aws_http_stream_activate(stream->stream);
}
