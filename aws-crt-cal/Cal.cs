/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System.Runtime.InteropServices;
using System.Security;

namespace Aws.Crt.Cal
{
    [SecuritySafeCritical]
    internal class LibraryHandle
    {
        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        delegate void AwsDotnetCalLibraryInit();

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        delegate void AwsDotnetCalLibraryCleanUp();

        private AwsDotnetCalLibraryInit Init = NativeAPI.Bind<AwsDotnetCalLibraryInit>("aws_dotnet_cal_library_init");
        private AwsDotnetCalLibraryCleanUp CleanUp = NativeAPI.Bind<AwsDotnetCalLibraryCleanUp>("aws_dotnet_cal_library_clean_up");

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
