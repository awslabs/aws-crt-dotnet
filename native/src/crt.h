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

#include <aws/common/common.h>

struct aws_allocator *aws_dotnet_get_allocator(void);


#if defined(_MSC_VER)
#define AWS_NORETURN(...) __declspec(noreturn) __VA_ARGS__
#elif defined(__clang__) || defined(__GNUC__)
#define AWS_NORETURN(...) __VA_ARGS__ __attribute__((noreturn))
#endif

/* This will throw an exception via a callback into .NET. It will never
 * return, so you must be sure to do all cleanup before throwing */
AWS_NORETURN(void aws_dotnet_throw_exception(const char *message, ...));
