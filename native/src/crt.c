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

#include <aws/common/error.h>
#include <aws/io/io.h>
#include <aws/io/tls_channel_handler.h>
#include <aws/mqtt/mqtt.h>

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>

struct aws_allocator *aws_dotnet_get_allocator() {
    return aws_default_allocator();
}

typedef void(DOTNET_CALL *dotnet_exception_callback)(int, const char *, const char *);
static dotnet_exception_callback s_throw_exception = NULL;

AWS_DOTNET_API
void aws_dotnet_set_exception_callback(dotnet_exception_callback callback) {
    s_throw_exception = callback;
}

void aws_dotnet_throw_exception(int error_code, const char *message, ...) {
    AWS_FATAL_ASSERT(s_throw_exception != NULL);
    va_list args;
    va_start(args, message);
    char buf[1024];
    vsnprintf(buf, sizeof(buf), message, args);
    va_end(args);

    char exception[1280];
    snprintf(exception, sizeof(exception), "%s (aws_last_error: %s)", buf, aws_error_str(error_code));
    s_throw_exception(error_code, aws_error_name(error_code), exception);
}

AWS_DOTNET_API
void aws_dotnet_static_init(void) {
    aws_load_error_strings();
    aws_io_load_error_strings();
    aws_mqtt_load_error_strings();

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_tls_init_static_state(allocator);
}

AWS_DOTNET_API
void aws_dotnet_static_shutdown(void) {
    aws_tls_clean_up_static_state();
}

AWS_DOTNET_API
int aws_test_exception(int a, int b) {
    aws_dotnet_throw_exception(AWS_ERROR_UNSUPPORTED_OPERATION, "TEST EXCEPTION");
    return a * b;
}

AWS_DOTNET_API
void aws_test_exception_void(void) {
    aws_dotnet_throw_exception(AWS_ERROR_UNSUPPORTED_OPERATION, "TEST EXCEPTION VOID");
}

AWS_DOTNET_API
const char *aws_dotnet_error_string(int error_code) {
    return aws_error_str(error_code);
}

AWS_DOTNET_API
const char *aws_dotnet_error_name(int error_code) {
    return aws_error_name(error_code);
}
