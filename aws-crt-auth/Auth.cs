/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt;

namespace Aws.Crt.Auth
{
    [SecuritySafeCritical]
    internal class LibraryHandle
    {
        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        delegate void AwsDotnetAuthLibraryInit();

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        delegate void AwsDotnetAuthLibraryCleanUp();

        private AwsDotnetAuthLibraryInit Init = NativeAPI.Bind<AwsDotnetAuthLibraryInit>("aws_dotnet_auth_library_init");
        private AwsDotnetAuthLibraryCleanUp CleanUp = NativeAPI.Bind<AwsDotnetAuthLibraryCleanUp>("aws_dotnet_auth_library_clean_up");

        internal LibraryHandle()
        {
            Init();
        }

        ~LibraryHandle()
        {
            CleanUp();
        }
    }
}
