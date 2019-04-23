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
