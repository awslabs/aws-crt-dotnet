/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "aws/checksums/crc.h"
#include "crt.h"
#include "exports.h"

AWS_DOTNET_API
uint32_t aws_dotnet_crc32(const uint8_t *input, int length, uint32_t previous) {
    return aws_checksums_crc32_ex(input, (size_t)length, previous);
}

AWS_DOTNET_API
uint32_t aws_dotnet_crc32c(const uint8_t *input, int length, uint32_t previous) {
    return aws_checksums_crc32c_ex(input, (size_t)length, previous);
}

AWS_DOTNET_API
uint64_t aws_dotnet_crc64nvme(const uint8_t *input, int length, uint64_t previous) {
    return aws_checksums_crc64nvme_ex(input, (size_t)length, previous);
}
