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

namespace Aws.CRT.IO
{
    [StructLayout(LayoutKind.Sequential)]
    class AwsSocketOptions {
        public Int32 type;
        public Int32 domain;
        public UInt32 connect_timeout_ms;
        public UInt16 keep_alive_interval_sec;
        public UInt16 keep_alive_timeout_sec;
        public bool keepalive;
    }

    public enum SocketDomain {
        IPv4 = 0,
        IPv6 = 1,
        LOCAL = 2
    }

    public enum SocketType {
        STREAM = 0,
        DGRAM = 1
    }

    public class SocketOptions
    {
        internal static class API
        {
            public delegate Handle aws_dotnet_socket_options_new();
            public delegate void aws_dotnet_socket_options_destroy(IntPtr options);

            [SecuritySafeCritical]
            public static aws_dotnet_socket_options_new make_new = NativeAPI.Bind<aws_dotnet_socket_options_new>();
            [SecuritySafeCritical]
            public static aws_dotnet_socket_options_destroy destroy = NativeAPI.Bind<aws_dotnet_socket_options_destroy>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        private Handle nativeHandle;
        private AwsSocketOptions options;

        public SocketOptions()
        {
            nativeHandle = API.make_new();
            options = Marshal.PtrToStructure<AwsSocketOptions>(nativeHandle.DangerousGetHandle());
        }

        public SocketDomain Domain {
            get {
                return (SocketDomain)options.domain;
            }
            set {
                options.domain = (Int32)value;
            }
        }

        public SocketType Type {
            get {
                return (SocketType)options.type;
            }
            set {
                options.type = (Int32)value;
            }
        }

        public UInt32 ConnectTimeoutMs {
            get {
                return options.connect_timeout_ms;
            }
            set {
                options.connect_timeout_ms = value;
            }
        }

        public UInt16 KeepAliveIntervalSeconds {
            get {
                return options.keep_alive_interval_sec;
            }
            set {
                options.keep_alive_interval_sec = value;
            }
        }

        public UInt16 KeepAliveTimeoutSeconds {
            get {
                return options.keep_alive_timeout_sec;
            }
            set {
                options.keep_alive_timeout_sec = value;
            }
        }

        public bool KeepAlive {
            get {
                return options.keepalive;
            }
            set {
                options.keepalive = value;
            }
        }
    }
}