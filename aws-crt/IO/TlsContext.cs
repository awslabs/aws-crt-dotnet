/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Generic;
using System.Security;
using System.Runtime.InteropServices;

namespace Aws.Crt.IO
{
    public enum TlsVersions {
        SSLv3 = 0,
        TLSv1 = 1,
        TLSv1_1 = 2,
        TLSv1_2 = 3,
        TLSv1_3 = 4,
        SYS_DEFAULTS = 128
    }

    public class TlsContextOptions {
        public TlsVersions MinimumTlsVersion { get; set; } = TlsVersions.SYS_DEFAULTS;
        public string AlpnList { get; set; } = null;
        public uint MaxFragmentSize { get; set; } = 16 * 1024;
        public bool VerifyPeer { get; set; } = true;

        internal string caFile;
        internal string caPath;
        internal string certificatePath;
        internal string privateKeyPath;
        internal string pkcs12Path;
        internal string pkcs12Password;

        public TlsContextOptions() {
        }

        public static TlsContextOptions DefaultClient() {
            TlsContextOptions options = new TlsContextOptions();
            return options;
        }

        public static TlsContextOptions ClientMtlsFromPath(string certPath, string privateKeyPath) {
            TlsContextOptions options = new TlsContextOptions();
            options.InitClientMTlsFromPath(certPath, privateKeyPath);
            return options;
        }

        public static TlsContextOptions DefaultServerFromPath(string certPath, string privateKeyPath) {
            TlsContextOptions options = new TlsContextOptions();
            options.InitDefaultServerFromPath(certPath, privateKeyPath);
            return options;
        }

        public static TlsContextOptions ServerPkcs12FromPath(string pkcs12Path, string pkcs12Password) {
            TlsContextOptions options = new TlsContextOptions();
            options.InitServerPkcs12FromPath(pkcs12Path, pkcs12Password);
            return options;
        }

        public void OverrideDefaultTrustStoreFromPath(string caPath, string caFile) {
            this.caPath = caPath;
            this.caFile = caFile;
        }

        public void InitClientMTlsFromPath(string certPath, string privateKeyPath) {
            this.certificatePath = certPath;
            this.privateKeyPath = privateKeyPath;
        }

        public void InitDefaultServerFromPath(string certPath, string privateKeyPath) {
            this.certificatePath = certPath;
            this.privateKeyPath = privateKeyPath;
            VerifyPeer = false;
        }

        public void InitClientMtlsPkcs12FromPath(string pkcs12Path, string pkcs12Password) {
            this.pkcs12Path = pkcs12Path;
            this.pkcs12Password = pkcs12Password;
        }

        public void InitServerPkcs12FromPath(string pkcs12Path, string pkcs12Password) {
            this.pkcs12Path = pkcs12Path;
            this.pkcs12Password = pkcs12Password;
            VerifyPeer = false;
        }
    }

    public abstract class TlsContext {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate Handle aws_dotnet_tls_ctx_new_client(Int32 min_tls_version,
                                                                [MarshalAs(UnmanagedType.LPStr)] string ca_file,
                                                                [MarshalAs(UnmanagedType.LPStr)] string ca_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string alpn_list,
                                                                [MarshalAs(UnmanagedType.LPStr)] string cert_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string key_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string pkcs12_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string pkcs12_password,
                                                                UInt32 max_fragment_size,
                                                                byte verify_peer);
            public delegate Handle aws_dotnet_tls_ctx_new_server(Int32 min_tls_version,
                                                                [MarshalAs(UnmanagedType.LPStr)] string ca_file,
                                                                [MarshalAs(UnmanagedType.LPStr)] string ca_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string alpn_list,
                                                                [MarshalAs(UnmanagedType.LPStr)] string cert_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string key_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string pkcs12_path,
                                                                [MarshalAs(UnmanagedType.LPStr)] string pkcs12_password,
                                                                UInt32 max_fragment_size,
                                                                byte verify_peer);
            public delegate void aws_dotnet_tls_ctx_destroy(IntPtr ctx);

            public static aws_dotnet_tls_ctx_new_client make_new_client = NativeAPI.Bind<aws_dotnet_tls_ctx_new_client>();
            public static aws_dotnet_tls_ctx_new_server make_new_server = NativeAPI.Bind<aws_dotnet_tls_ctx_new_server>();
            public static aws_dotnet_tls_ctx_destroy destroy = NativeAPI.Bind<aws_dotnet_tls_ctx_destroy>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        internal Handle NativeHandle { get; set; }
    }

    public class ClientTlsContext : TlsContext {
        public ClientTlsContext(TlsContextOptions options) {
            NativeHandle = API.make_new_client(
                (Int32)options.MinimumTlsVersion,
                options.caFile, 
                options.caPath, 
                options.AlpnList, 
                options.certificatePath,
                options.privateKeyPath, 
                options.pkcs12Path, 
                options.pkcs12Password,
                options.MaxFragmentSize, 
                (byte)(options.VerifyPeer ? 1 : 0));
        }
    }

    public class ServerTlsContext : TlsContext {
        public ServerTlsContext(TlsContextOptions options) {
            NativeHandle = API.make_new_server(
                (Int32)options.MinimumTlsVersion,
                options.caFile,
                options.caPath,
                options.AlpnList,
                options.certificatePath,
                options.privateKeyPath,
                options.pkcs12Path,
                options.pkcs12Password,
                options.MaxFragmentSize,
                (byte)(options.VerifyPeer ? 1 : 0));
        }
    }

    public class TlsConnectionOptions
    {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate Handle aws_dotnet_tls_connection_options_new(
                                    IntPtr tlsContext,
                                    [MarshalAs(UnmanagedType.LPStr)] string serverName,
                                    [MarshalAs(UnmanagedType.LPStr)] string alpnList);
            public delegate void aws_dotnet_tls_connection_options_destroy(IntPtr options);

            public static aws_dotnet_tls_connection_options_new make_new = NativeAPI.Bind<aws_dotnet_tls_connection_options_new>();
            public static aws_dotnet_tls_connection_options_destroy destroy = NativeAPI.Bind<aws_dotnet_tls_connection_options_destroy>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        internal Handle NativeHandle
        {
            get
            {
                return API.make_new(
                    Context.NativeHandle.DangerousGetHandle(),
                    ServerName, 
                    AlpnList);
            }
        }

        public TlsContext Context { get; set; }
        public string ServerName { get; set; }
        public string AlpnList { get; set; }

        public TlsConnectionOptions(TlsContext context)
        {
            this.Context = context;
        }
    }
}
