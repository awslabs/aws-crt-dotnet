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

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>

struct aws_allocator *aws_dotnet_get_allocator() {
    return aws_default_allocator();
}

typedef void(DOTNET_CALL *dotnet_exception_callback)(int, const char*, const char *);
static dotnet_exception_callback s_throw_exception = NULL;

AWS_DOTNET_API
void aws_dotnet_set_exception_callback(dotnet_exception_callback callback) {
    s_throw_exception = callback;
}

void aws_dotnet_throw_exception(const char *message, ...) {
    AWS_FATAL_ASSERT(s_throw_exception != NULL && "Exception handler not installed");
    va_list args;
    va_start(args, message);
    char buf[1024];
    vsnprintf(buf, sizeof(buf), message, args);
    va_end(args);

    char exception[1280];
    int error_code = aws_last_error();
    snprintf(exception, sizeof(exception), "%s (aws_last_error: %s)", buf, aws_error_str(error_code));
    s_throw_exception(error_code, aws_error_str(error_code), exception);
}

AWS_DOTNET_API
int aws_test_exception(int a, int b) {
    aws_dotnet_throw_exception("TEST EXCEPTION");
    return a * b;
}

AWS_DOTNET_API
void aws_test_exception_void(void) {
    aws_dotnet_throw_exception("TEST EXCEPTION VOID");
}
