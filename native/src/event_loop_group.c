/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "crt.h"
#include "exports.h"

#include <aws/io/event_loop.h>

AWS_DOTNET_API
struct aws_event_loop_group *aws_dotnet_event_loop_group_new_default(int num_threads) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_event_loop_group *elg = aws_event_loop_group_new_default(allocator, (uint16_t)num_threads, NULL);
    if (elg == NULL) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to create aws_event_loop_group");
    }

    return elg;
}

AWS_DOTNET_API
void aws_dotnet_event_loop_group_destroy(struct aws_event_loop_group *elg) {
    aws_event_loop_group_release(elg);
}
