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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

using Aws.Crt;
using Aws.Crt.IO;

namespace Aws.Crt.Http
{
    public sealed class HttpClientConnectionManagerOptions
    {
        public ClientBootstrap Bootstrap;
        public String Host;
        public UInt16 Port;
        public Int32 MaxConnections;
        public UInt64 InitialWindowSize;
        public SocketOptions SocketOptions;
        public TlsConnectionOptions TlsConnectionOptions;
        // TODO: Proxy support
    }

    public sealed class HttpClientConnectionManager
    {
        [SecuritySafeCritical]
        internal static class API
        {
            static private LibraryHandle library = new LibraryHandle();
            public delegate Handle aws_dotnet_http_client_connection_manager_new(
                                    IntPtr clientBootstrap,
                                    [MarshalAs(UnmanagedType.LPStr)] string hostName,
                                    UInt16 port,
                                    IntPtr socketOptions,
                                    IntPtr tlsConnectionOptions,
                                    Int32  maxConnections,
                                    UInt64 initialWindowSize);
            public delegate void aws_dotnet_http_client_connection_manager_destroy(IntPtr manager);

            public static aws_dotnet_http_client_connection_manager_new make_new = NativeAPI.Bind<aws_dotnet_http_client_connection_manager_new>();
            public static aws_dotnet_http_client_connection_manager_destroy destroy = NativeAPI.Bind<aws_dotnet_http_client_connection_manager_destroy>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        internal Handle NativeHandle { get; private set; }
        private HttpClientConnectionManagerOptions options;

        public HttpClientConnectionManager(HttpClientConnectionManagerOptions options) {
            this.options = options;
            NativeHandle = API.make_new(
                options.Bootstrap.NativeHandle.DangerousGetHandle(), 
                options.Host, options.Port, 
                options.SocketOptions.NativeHandle.DangerousGetHandle(), 
                options.TlsConnectionOptions.NativeHandle.DangerousGetHandle(), 
                options.MaxConnections, options.InitialWindowSize);

        }


    }
}
