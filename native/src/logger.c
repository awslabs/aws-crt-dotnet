/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
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
    struct aws_logger_standard_options options;
    options.level = (enum aws_log_level)level;
    options.file = file;
    options.filename = filename;

    if (aws_logger_init_standard(&s_logger, allocator, &options)) {
        aws_dotnet_throw_exception(aws_last_error(), "Unable to initialize logging");
        return;
    }

    aws_logger_set(&s_logger);

    aws_io_load_log_subject_strings();
}
