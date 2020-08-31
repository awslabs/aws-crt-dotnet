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
using System.Security;
using System.Runtime.InteropServices;

namespace Aws.Crt.IO
{
    public class ClientBootstrap
    {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate Handle aws_dotnet_client_bootstrap_new(IntPtr eventLoopGroup, IntPtr hostResolver);
            public delegate void aws_dotnet_client_bootstrap_destroy(IntPtr clientBootstrap);

            public static aws_dotnet_client_bootstrap_new make_new = NativeAPI.Bind<aws_dotnet_client_bootstrap_new>();
            public static aws_dotnet_client_bootstrap_destroy destroy = NativeAPI.Bind<aws_dotnet_client_bootstrap_destroy>();
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

        public ClientBootstrap(EventLoopGroup eventLoopGroup, HostResolver hostResolver = null)
        {
            if (hostResolver == null) {
                hostResolver = new DefaultHostResolver(eventLoopGroup);
            }
            
            NativeHandle = API.make_new(eventLoopGroup.NativeHandle.DangerousGetHandle(), hostResolver.NativeHandle.DangerousGetHandle());
        }
    }
}
