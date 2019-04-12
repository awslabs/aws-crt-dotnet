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
    public enum TlsVersions {
        SSLv3 = 0,
        TLSv1 = 1,
        TLSv1_1 = 2,
        TLSv1_2 = 3,
        TLSv1_3 = 4,
        SYS_DEFAULTS = 128
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    class AwsTlsCtxOptions {
        public Int32 minimum_tls_version;
        public IntPtr ca_file;
        public IntPtr ca_path;
        public IntPtr alpn_list;
        public IntPtr certificate_path;
        public IntPtr private_key_path;
        public IntPtr pkcs12_path;
        public IntPtr pkcs12_password;
        public IntPtr max_fragment_size; // size_t
        public bool verify_peer;
    }

    public class TlsContextOptions {
        internal static class API
        {
            public delegate Handle aws_dotnet_tls_ctx_options_new_default_client();
            public delegate void aws_dotnet_tls_ctx_options_destroy(IntPtr options);
            public delegate void aws_dotnet_tls_ctx_options_set_alpn_list(IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string alpn);

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_new_default_client make_new_default_client = NativeAPI.Bind<aws_dotnet_tls_ctx_options_new_default_client>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_destroy destroy = NativeAPI.Bind<aws_dotnet_tls_ctx_options_destroy>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_set_alpn_list set_alpn_list = NativeAPI.Bind<aws_dotnet_tls_ctx_options_set_alpn_list>();
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
        private AwsTlsCtxOptions options;

        public TlsContextOptions() {
            nativeHandle = API.make_new_default_client();
            options = Marshal.PtrToStructure<AwsTlsCtxOptions>(nativeHandle.DangerousGetHandle());
        }

        public TlsVersions MinimumTlsVersion {
            get {
                return (TlsVersions)options.minimum_tls_version;
            }
            set {
                options.minimum_tls_version = (Int32)value;
            }
        }

        public string AlpnList {
            get {
                return Marshal.PtrToStringAnsi(options.alpn_list);
            }
            set {
                API.set_alpn_list(nativeHandle.DangerousGetHandle(), value);
                Marshal.PtrToStructure<AwsTlsCtxOptions>(nativeHandle.DangerousGetHandle(), options);
            }
        }
    }
}