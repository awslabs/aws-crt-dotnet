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

#include <aws/io/logging.h>

static struct aws_logger s_logger;

AWS_DOTNET_API void aws_dotnet_logger_enable(int level, const char *filename) {
    if (aws_logger_get() == &s_logger) {
        aws_logger_set(NULL);
        aws_logger_clean_up(&s_logger);
        if (level == AWS_LL_NONE) {
            AWS_ZERO_STRUCT(s_logger);
            return;
        }
    }

    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    FILE *file = (filename) ? NULL : stdout;
    struct aws_logger_standard_options options = {
        .level = (enum aws_log_level)level, 
        .file = file,
        .filename = filename
    };
    if (aws_logger_init_standard(&s_logger, allocator, &options)) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize logging");
        return;
    }

    aws_logger_set(&s_logger);

    aws_io_load_log_subject_strings();
}
