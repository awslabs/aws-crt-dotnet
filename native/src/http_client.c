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

typedef void(aws_dotnet_http_on_client_connection_setup_fn)(int error_code);

typedef void(aws_dotnet_http_on_client_connection_shutdown_fn)(int error_code);

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
        aws_mem_acquire(allocator, sizeof(struct aws_dotnet_http_connection));
    if (!connection) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to allocate new aws_dotnet_http_connection");
        return NULL;
    }
    AWS_ZERO_STRUCT(*connection);
    struct aws_http_client_connection_options options = AWS_HTTP_CLIENT_CONNECTION_OPTIONS_INIT;
    options.allocator = allocator;
    options.bootstrap = client_bootstrap;
    if (initial_window_size != 0) {
        options.initial_window_size = initial_window_size;
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
typedef void(aws_dotnet_http_on_incoming_headers_fn)(struct aws_dotnet_http_header headers[], uint32_t header_count);
typedef void(aws_dotnet_http_on_incoming_header_block_done_fn)(bool has_body);
typedef void(aws_dotnet_http_on_incoming_body_fn)(uint8_t *data, uint64_t size, uint64_t *window_size);
typedef void(aws_dotnet_http_on_stream_complete_fn)(int error_code);

struct aws_dotnet_http_stream {
    struct aws_dotnet_http_connection *connection;
    struct aws_http_stream *stream;
    aws_dotnet_http_stream_outgoing_body_fn *stream_outgoing_body;
    aws_dotnet_http_on_incoming_headers_fn *on_incoming_headers;
    aws_dotnet_http_on_incoming_header_block_done_fn *on_incoming_headers_block_done;
    aws_dotnet_http_on_incoming_body_fn *on_incoming_body;
    aws_dotnet_http_on_stream_complete_fn *on_stream_complete;
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
    buf->len += bytes_written;
    return state;
}

static void s_stream_on_incoming_headers(
    struct aws_http_stream *s,
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

    stream->on_incoming_headers(dotnet_headers, (uint32_t)header_count);
    for (size_t idx = 0; idx < header_count * 2; ++idx) {
        aws_string_destroy(header_strings[idx]);
    }
}

static void s_stream_on_incoming_header_block_done(struct aws_http_stream *s, bool has_body, void *user_data) {
    (void)s;
    struct aws_dotnet_http_stream *stream = user_data;
    if (stream->on_incoming_headers_block_done) {
        stream->on_incoming_headers_block_done(has_body);
    }
}

static void s_stream_on_incoming_body(
    struct aws_http_stream *s,
    const struct aws_byte_cursor *data,
    size_t *out_window_update_size,
    void *user_data) {
    (void)s;
    (void)out_window_update_size;
    struct aws_dotnet_http_stream *stream = user_data;
    if (stream->on_incoming_body) {
        uint64_t window_update_size = *out_window_update_size;
        stream->on_incoming_body(data->ptr, data->len, &window_update_size);
        *out_window_update_size = window_update_size;
    }
}

static void s_stream_on_stream_complete(struct aws_http_stream *s, int error_code, void *user_data) {
    (void)s;
    struct aws_dotnet_http_stream *stream = user_data;
    stream->on_stream_complete(error_code);
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
    struct aws_http_request_options options = AWS_HTTP_REQUEST_OPTIONS_INIT;
    options.client_connection = connection->connection;
    options.uri = aws_byte_cursor_from_c_str(uri);
    options.method = aws_byte_cursor_from_c_str(method);
    options.header_array = NULL;
    options.num_headers = 0;
    AWS_VARIABLE_LENGTH_ARRAY(struct aws_http_header, req_headers, header_count ? header_count : 1);
    if (header_count > 0) {
        for (size_t header_idx = 0; header_idx < header_count; ++header_idx) {
            req_headers[header_idx].name = aws_byte_cursor_from_c_str(headers[header_idx].name);
            req_headers[header_idx].value = aws_byte_cursor_from_c_str(headers[header_idx].value);
        }
        options.header_array = req_headers;
        options.num_headers = header_count;
    }
    options.stream_outgoing_body = s_stream_stream_outgoing_body;
    options.on_response_headers = s_stream_on_incoming_headers;
    options.on_response_header_block_done = s_stream_on_incoming_header_block_done;
    options.on_response_body = s_stream_on_incoming_body;
    options.on_complete = s_stream_on_stream_complete;

    struct aws_dotnet_http_stream *stream = aws_mem_acquire(allocator, sizeof(struct aws_dotnet_http_stream));
    if (!stream) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to allocate aws_dotnet_http_stream");
        goto error;
    }

    options.user_data = stream;

    stream->stream = aws_http_stream_new_client_request(&options);
    if (!stream->stream) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize new aws_http_stream");
        goto error;
    }

    stream->connection = connection;
    stream->stream_outgoing_body = stream_outgoing_body;
    stream->on_incoming_headers = on_incoming_headers;
    stream->on_incoming_headers_block_done = on_incoming_headers_block_done;
    stream->on_incoming_body = on_incoming_body;
    stream->on_stream_complete = on_stream_complete;

    return stream;

error:
    if (stream) {
        aws_mem_release(allocator, stream);
    }
    return NULL;
}

AWS_DOTNET_API void aws_dotnet_http_stream_destroy(struct aws_dotnet_http_stream *stream) {
    if (!stream) {
        return;
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_http_stream_release(stream->stream);
    aws_mem_release(allocator, stream);
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
