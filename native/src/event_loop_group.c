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
    struct aws_event_loop_group *elg = aws_mem_calloc(allocator, 1, sizeof(struct aws_event_loop_group));
    if (!elg) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to create aws_event_loop_group");
        goto error;
    }
    if (aws_event_loop_group_default_init(elg, allocator, (uint16_t)num_threads)) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize aws_event_loop_group");
        goto error;
    }
    return elg;
error:
    if (elg) {
        aws_mem_release(allocator, elg);
    }
    return NULL;
}

AWS_DOTNET_API
void aws_dotnet_event_loop_group_destroy(struct aws_event_loop_group *elg) {
    if (!elg) {
        return;
    }
    aws_event_loop_group_clean_up(elg);
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_mem_release(allocator, elg);
}
