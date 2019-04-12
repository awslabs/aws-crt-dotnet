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

    // [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    // class AwsTlsCtxOptions {
    //     public Int32 minimum_tls_version;
    //     public IntPtr ca_file;
    //     public IntPtr ca_path;
    //     public IntPtr alpn_list;
    //     public IntPtr certificate_path;
    //     public IntPtr private_key_path;
    //     public IntPtr pkcs12_path;
    //     public IntPtr pkcs12_password;
    //     public IntPtr max_fragment_size; // size_t
    //     public bool verify_peer;
    // }

    public class TlsContextOptions {
        internal static class API
        {
            public delegate Handle aws_dotnet_tls_ctx_options_new_default_client();
            public delegate Handle aws_dotnet_tls_ctx_options_new_default_server(
                [MarshalAs(UnmanagedType.LPStr)] string cert_path, [MarshalAs(UnmanagedType.LPStr)] string key_path);
            public delegate void aws_dotnet_tls_ctx_options_destroy(IntPtr options);
            
            public delegate void aws_dotnet_tls_ctx_options_set_minimum_tls_version(IntPtr options, Int32 version);
            public delegate Int32 aws_dotnet_tls_ctx_options_get_minimum_tls_version(IntPtr options);

            public delegate void aws_dotnet_tls_ctx_options_set_alpn_list(IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string alpn);
            public delegate IntPtr aws_dotnet_tls_ctx_options_get_alpn_list(IntPtr options);

            public delegate void aws_dotnet_tls_ctx_options_set_max_fragment_size(IntPtr options, IntPtr maxFragmentSize);
            public delegate IntPtr aws_dotnet_tls_ctx_options_get_max_fragment_size(IntPtr options);

            public delegate void aws_dotnet_tls_ctx_options_set_verify_peer(IntPtr options, bool verify);
            public delegate bool aws_dotnet_tls_ctx_options_get_verify_peer(IntPtr options);

            public delegate void aws_dotnet_tls_ctx_options_override_default_trust_store_from_path(
                IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string ca_path, [MarshalAs(UnmanagedType.LPStr)] string ca_file);

            public delegate void aws_dotnet_tls_ctx_options_init_client_mtls_from_path(
                IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string cert_path, [MarshalAs(UnmanagedType.LPStr)] string key_path);

            public delegate void aws_dotnet_tls_ctx_options_init_default_server_from_path(
                IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string cert_path, [MarshalAs(UnmanagedType.LPStr)] string key_path);

            public delegate void aws_dotnet_tls_ctx_options_init_client_mtls_pkcs12_from_path(
                IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string cert_path, [MarshalAs(UnmanagedType.LPStr)] string key_path);

            public delegate void aws_dotnet_tls_ctx_options_init_server_pkcs12_from_path(
                IntPtr options, [MarshalAs(UnmanagedType.LPStr)] string cert_path, [MarshalAs(UnmanagedType.LPStr)] string key_path);

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_new_default_client make_new_default_client = 
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_new_default_client>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_new_default_server make_new_default_server =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_new_default_server>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_destroy destroy = 
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_destroy>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_set_minimum_tls_version set_minimum_tls_version = 
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_set_minimum_tls_version>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_get_minimum_tls_version get_minimum_tls_version = 
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_get_minimum_tls_version>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_set_alpn_list set_alpn_list = 
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_set_alpn_list>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_get_alpn_list get_alpn_list = 
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_get_alpn_list>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_set_max_fragment_size set_max_fragment_size =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_set_max_fragment_size>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_get_max_fragment_size get_max_fragment_size =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_get_max_fragment_size>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_set_verify_peer set_verify_peer =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_set_verify_peer>();
            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_get_verify_peer get_verify_peer =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_get_verify_peer>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_override_default_trust_store_from_path override_default_trust_store_from_path =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_override_default_trust_store_from_path>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_init_client_mtls_from_path init_client_mtls_from_path =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_init_client_mtls_from_path>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_init_default_server_from_path init_default_server_from_path =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_init_default_server_from_path>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_init_client_mtls_pkcs12_from_path init_client_mtls_pkcs12_from_path =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_init_client_mtls_pkcs12_from_path>();

            [SecuritySafeCritical]
            public static aws_dotnet_tls_ctx_options_init_server_pkcs12_from_path init_server_pkcs12_from_path =
                NativeAPI.Bind<aws_dotnet_tls_ctx_options_init_server_pkcs12_from_path>();
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
        private IntPtr nativePtr {
            get { return nativeHandle.DangerousGetHandle(); }
        }

        public TlsContextOptions() {
            nativeHandle = API.make_new_default_client();
        }

        public TlsContextOptions(string certPath, string privateKeyPath) {
            nativeHandle = API.make_new_default_server(certPath, privateKeyPath);
        }

        public static TlsContextOptions ClientMtlsFromPath(string certPath, string privateKeyPath) {
            TlsContextOptions options = new TlsContextOptions();
            options.InitClientMTlsFromPath(certPath, privateKeyPath);
            return options;
        }

        public static TlsContextOptions DefaultServerFromPath(string certPath, string privateKeyPath) {
            TlsContextOptions options = new TlsContextOptions(certPath, privateKeyPath);
            return options;
        }

        public static TlsContextOptions ServerPkcs12FromPath(string pkcs12Path, string pkcs12Password) {
            TlsContextOptions options = new TlsContextOptions();
            options.InitServerPkcs12FromPath(pkcs12Path, pkcs12Password);
            return options;
        }

        public TlsVersions MinimumTlsVersion {
            get {
                return (TlsVersions)API.get_minimum_tls_version(nativePtr);
            }
            set {
                API.set_minimum_tls_version(nativePtr, (Int32)value);
            }
        }

        public string AlpnList {
            get {
                return Marshal.PtrToStringAnsi(API.get_alpn_list(nativePtr));
            }
            set {
                API.set_alpn_list(nativePtr, value);
            }
        }

        public uint MaxFragmentSize {
            get {
                return (uint)API.get_max_fragment_size(nativePtr);
            }
            set {
                API.set_max_fragment_size(nativePtr, (IntPtr)value);
            }
        }

        public bool VerifyPeer {
            get {
                return API.get_verify_peer(nativePtr);
            }
            set {
                API.set_verify_peer(nativePtr, value);
            }
        }

        public void OverrideDefaultTrustStoreFromPath(string caPath, string caFile) {
            API.override_default_trust_store_from_path(nativePtr, caPath, caFile);
        }

        public void InitClientMTlsFromPath(string certPath, string privateKeyPath) {
            API.init_client_mtls_from_path(nativePtr, certPath, privateKeyPath);
        }

        public void InitDefaultServerFromPath(string certPath, string privateKeyPath) {
            API.init_default_server_from_path(nativePtr, certPath, privateKeyPath);
        }

        public void InitClientMtlsPkcs12FromPath(string pkcs12Path, string pkcs12Password) {
            API.init_client_mtls_pkcs12_from_path(nativePtr, pkcs12Path, pkcs12Password);
        }

        public void InitServerPkcs12FromPath(string pkcs12Path, string pkcs12Password) {
            API.init_server_pkcs12_from_path(nativePtr, pkcs12Path, pkcs12Password);
        }
    }
}