/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
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
void aws_dotnet_throw_exception(int error_code, const char *message, ...);
