/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Aws.Crt.IO {
    public class EventLoopGroup {

        [SecuritySafeCritical]
        internal static class API
        {
            public delegate Handle aws_dotnet_event_loop_group_new_default(int numThreads);
            public delegate void aws_dotnet_event_loop_group_destroy(IntPtr elg);

            public static aws_dotnet_event_loop_group_new_default make_new_default = NativeAPI.Bind<aws_dotnet_event_loop_group_new_default>();
            public static aws_dotnet_event_loop_group_destroy destroy = NativeAPI.Bind<aws_dotnet_event_loop_group_destroy>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle() {
                API.destroy(handle);
                return true;
            }
        }
        
        internal Handle NativeHandle { get; private set; }

        public EventLoopGroup(int numThreads=1) {
            NativeHandle = API.make_new_default(numThreads);
        }
    }
 }
 