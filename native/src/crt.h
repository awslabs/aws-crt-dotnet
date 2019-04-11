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

/* Win32 .NET callbacks are __stdcall, everything else is __cdecl */
#if defined(_MSC_VER) && !defined(_WIN64)
#    define DOTNET_CALL __stdcall
#else
#    define DOTNET_CALL
#endif

struct aws_allocator *aws_dotnet_get_allocator(void);

/* This will record an exception message via a callback into .NET. When the
 * native function returns, the exception will be thrown, which preserves the
 * .NET callstack */
void aws_dotnet_throw_exception(const char *message, ...);
