/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt;

namespace Aws.Crt.Auth
{
    [SecuritySafeCritical]
    internal class LibraryHandle
    {
        delegate void aws_dotnet_auth_library_init();
        delegate void aws_dotnet_auth_library_clean_up();

        private aws_dotnet_auth_library_init Init = NativeAPI.Bind<aws_dotnet_auth_library_init>();
        private aws_dotnet_auth_library_clean_up CleanUp = NativeAPI.Bind<aws_dotnet_auth_library_clean_up>();

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