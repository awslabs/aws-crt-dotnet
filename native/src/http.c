/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "crt.h"
#include "exports.h"

#include <aws/http/http.h>

AWS_DOTNET_API
void aws_dotnet_http_library_init(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_http_library_init(allocator);
}

AWS_DOTNET_API
void aws_dotnet_http_library_clean_up(void) {
    aws_http_library_clean_up();
}

AWS_DOTNET_API
const char *aws_dotnet_http_status_text(int status_code) {
    return aws_http_status_text(status_code);
}
