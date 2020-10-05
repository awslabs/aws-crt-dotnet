/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Aws.Crt.IO
{
    public enum SocketDomain {
        IPv4 = 0,
        IPv6 = 1,
        Local = 2
    }

    public enum SocketType {
        Stream = 0,
        Dgram = 1
    }

    public class SocketOptions
    {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate Handle aws_dotnet_socket_options_new(
                                    Int32 type, 
                                    Int32 domain, 
                                    UInt32 connect_timeout_ms, 
                                    UInt16 keep_alive_interval_sec, 
                                    UInt16 keep_alive_timeout_sec, 
                                    UInt16 keep_alive_max_failed_probes, 
                                    byte keepalive);
            public delegate void aws_dotnet_socket_options_destroy(IntPtr options);

            public static aws_dotnet_socket_options_new make_new = NativeAPI.Bind<aws_dotnet_socket_options_new>();
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

        internal Handle NativeHandle { 
            get {
                return API.make_new(
                            (Int32)Type,
                            (Int32)Domain,
                            ConnectTimeoutMs,
                            KeepAliveIntervalSeconds,
                            KeepAliveTimeoutSeconds,
                            0,
                            (byte)(KeepAlive ? 1: 0)
                );
            }        
        }

        public SocketType Type { get; set; }
        public SocketDomain Domain { get; set; }
        public UInt32 ConnectTimeoutMs { get; set; }
        public UInt16 KeepAliveIntervalSeconds { get; set; }
        public UInt16 KeepAliveTimeoutSeconds { get; set; }
        public bool KeepAlive { get; set; }
    }
}
