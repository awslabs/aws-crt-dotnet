/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Aws.Crt.IO
{
    public abstract class HostResolver
    {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate Handle aws_dotnet_host_resolver_new_default(IntPtr eventLoopGroup, int maxHosts);
            public delegate void aws_dotnet_host_resolver_destroy(IntPtr hostResolver);

            public static aws_dotnet_host_resolver_new_default make_new_default = NativeAPI.Bind<aws_dotnet_host_resolver_new_default>();
            public static aws_dotnet_host_resolver_destroy destroy = NativeAPI.Bind<aws_dotnet_host_resolver_destroy>();
        }

        public class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        public Handle NativeHandle { get; private set; }

        internal HostResolver(Handle handle) 
        {
            this.NativeHandle = handle;
        }
    }

    public sealed class DefaultHostResolver : HostResolver
    {
        public DefaultHostResolver(EventLoopGroup eventLoopGroup, int maxHosts=64)
            : base(API.make_new_default(eventLoopGroup.NativeHandle.DangerousGetHandle(), maxHosts))
        {
        }
    }
}