/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

#include "aws/cal/hash.h"
#include "crt.h"
#include "exports.h"

AWS_DOTNET_API
struct aws_hash *aws_dotnet_sha1_new(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    return aws_sha1_new(allocator);
}

AWS_DOTNET_API
struct aws_hash *aws_dotnet_sha256_new(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    return aws_sha256_new(allocator);
}

AWS_DOTNET_API
struct aws_hash *aws_dotnet_md5_new(void) {
    struct aws_allocator *allocator = aws_dotnet_get_allocator();
    return aws_md5_new(allocator);
}

AWS_DOTNET_API
int aws_dotnet_hash_update(struct aws_hash *hash, uint8_t *buffer, uint32_t buffer_size) {
    struct aws_byte_cursor buffer_cursor;
    AWS_ZERO_STRUCT(buffer_cursor);
    buffer_cursor.ptr = buffer;
    buffer_cursor.len = buffer_size;
    return aws_hash_update(hash, &buffer_cursor);
}

AWS_DOTNET_API
void aws_dotnet_hash_digest(struct aws_hash *hash, size_t truncate_to, uint8_t *buffer, uint32_t buffer_size) {
    (void)buffer_size;

    struct aws_byte_buf digest_buf = aws_byte_buf_from_array(buffer, hash->digest_size);
    digest_buf.len = 0;
    aws_hash_finalize(hash, &digest_buf, truncate_to);
}

AWS_DOTNET_API
void aws_dotnet_hash_destroy(struct aws_hash *hash) {
    aws_hash_destroy(hash);
}
