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

#include <aws/io/event_loop.h>

AWS_DOTNET_API
struct aws_event_loop_group *aws_dotnet_event_loop_group_new_default(int num_threads) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    struct aws_event_loop_group *elg = aws_mem_acquire(allocator, sizeof(struct aws_event_loop_group));
    if (!elg) {
        // TODO: Throw runtime exception
        goto error;
    }
    if (aws_event_loop_group_default_init(elg, allocator, (uint16_t)num_threads)) {
        // TODO: throw runtime exception
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
void aws_dotnet_event_loop_group_clean_up(struct aws_event_loop_group *elg) {
    if (!elg) {
        return;
    }
    aws_event_loop_group_clean_up(elg);
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    aws_mem_release(allocator, elg);
}
