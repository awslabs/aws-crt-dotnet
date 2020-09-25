/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "crt.h"
#include "exports.h"

#include <aws/auth/auth.h>

AWS_DOTNET_API
void aws_dotnet_auth_library_init(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_auth_library_init(allocator);
}

AWS_DOTNET_API
void aws_dotnet_auth_library_clean_up(void) {
    aws_auth_library_clean_up();
}

