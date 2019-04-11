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

namespace Aws.CRT.IO {
    public class EventLoopGroup {

        internal static class API
        {
            public delegate Handle aws_dotnet_event_loop_group_new_default(int numThreads);
            public delegate void aws_dotnet_event_loop_group_clean_up(IntPtr elg);

            [SecuritySafeCritical]
            public static aws_dotnet_event_loop_group_new_default new_default = NativeAPI.Bind<aws_dotnet_event_loop_group_new_default>();
            [SecuritySafeCritical] 
            public static aws_dotnet_event_loop_group_clean_up clean_up = NativeAPI.Bind<aws_dotnet_event_loop_group_clean_up>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle() {
                API.clean_up(handle);
                return true;
            }
        }
        
        internal Handle NativeHandle { get; private set; }

        public EventLoopGroup(int numThreads=1) {
            NativeHandle = API.new_default(numThreads);
        }
    }
 }