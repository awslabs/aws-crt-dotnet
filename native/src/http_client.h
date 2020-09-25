/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#ifndef AWS_DOTNET_HTTP_CLIENT_H
#define AWS_DOTNET_HTTP_CLIENT_H

#include <aws/common/common.h>

struct aws_dotnet_http_header {
    const char *name;
    const char *value;
};

#endif /* AWS_DOTNET_HTTP_CLIENT_H */
