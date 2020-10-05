/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
#include "crt.h"
#include "exports.h"

#include <aws/common/environment.h>
#include <aws/common/string.h>
#include <aws/http/http.h>

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>

static int s_memory_tracing = 0;

static struct aws_allocator *s_init_allocator(void) {
    if (s_memory_tracing) {
        struct aws_allocator *allocator = aws_default_allocator();
        allocator = aws_mem_tracer_new(allocator, NULL, (enum aws_mem_trace_level)s_memory_tracing, 8);
        return allocator;
    }
    return aws_default_allocator();
}

static struct aws_allocator *s_allocator = NULL;
struct aws_allocator *aws_dotnet_get_allocator() {
    if (AWS_UNLIKELY(s_allocator == NULL)) {
        s_allocator = s_init_allocator();
    }
    return s_allocator;
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

AWS_STATIC_STRING_FROM_LITERAL(s_debug_wait_environment_variable_name, "AWS_CRT_DEBUG_WAIT");

static void s_debug_wait(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_string *wait_value = NULL;
    aws_get_environment_value(allocator, s_debug_wait_environment_variable_name, &wait_value);
    if (wait_value != NULL && wait_value->len > 0) {
        bool done = false;
        while (!done) {
            ;
        }
    }

    aws_string_destroy(wait_value);
}

AWS_DOTNET_API
void aws_dotnet_static_init(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();

    s_debug_wait();

    aws_http_library_init(allocator);
}

AWS_DOTNET_API
void aws_dotnet_static_shutdown(void) {}

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
