/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt.IO;

namespace Aws.Crt.Http
{
    public static class Http
    {
        public delegate string aws_dotnet_http_status_text(int statusCode);
        public static aws_dotnet_http_status_text GetStatusText = NativeAPI.Bind<aws_dotnet_http_status_text>();
    }

    [SecuritySafeCritical]
    internal class LibraryHandle
    {
        delegate void aws_dotnet_http_library_init();
        delegate void aws_dotnet_http_library_clean_up();

        private aws_dotnet_http_library_init Init = NativeAPI.Bind<aws_dotnet_http_library_init>();
        private aws_dotnet_http_library_clean_up CleanUp = NativeAPI.Bind<aws_dotnet_http_library_clean_up>();

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