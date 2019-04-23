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

#include <aws/http/connection.h>
#include <aws/io/socket.h>

typedef void(
    aws_dotnet_http_on_client_connection_setup_fn)(int error_code);

typedef void(
    aws_dotnet_http_on_client_connection_shutdown_fn)(int error_code);

struct aws_dotnet_http_connection {
    struct aws_http_connection *connection;
    aws_dotnet_http_on_client_connection_setup_fn *on_setup;
    aws_dotnet_http_on_client_connection_shutdown_fn *on_shutdown;
};

static void s_http_connection_on_setup(struct aws_http_connection *connection, int error_code, void *user_data) {
    (void)connection;
    struct aws_dotnet_http_connection *dotnet_connection = user_data;
    dotnet_connection->on_setup(error_code);
}

static void s_http_connection_on_shutdown(struct aws_http_connection *connection, int error_code, void *user_data) {
    (void)connection;
    struct aws_dotnet_http_connection *dotnet_connection = user_data;
    dotnet_connection->on_shutdown(error_code);
}

static struct aws_socket_options s_default_socket_options = {.type = AWS_SOCKET_STREAM,
                                                           .domain = AWS_SOCKET_IPV4,
                                                           .connect_timeout_ms = 3000};

AWS_DOTNET_API
struct aws_dotnet_http_connection *
    aws_dotnet_http_connection_new(
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
