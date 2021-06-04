/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

using System;
using System.Runtime.InteropServices;

namespace Aws.Crt.Cal
{
    public class Hash
    {
        internal static class API
        {
            public delegate Handle aws_dotnet_sha1_new();
            public delegate Handle aws_dotnet_sha256_new();
            public delegate Handle aws_dotnet_md5_new();
            public delegate int aws_dotnet_hash_update(IntPtr hash,
                                                      [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.U1)] byte[] buffer,
                                                       UInt32 buffer_length);
            public delegate void aws_dotnet_hash_digest(IntPtr hash, UInt32 truncate, 
                        [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3, ArraySubType = UnmanagedType.U1)] byte[] buffer,
                                                       UInt32 buffer_length);
            public delegate void aws_dotnet_hash_destroy(IntPtr hash);
            public static aws_dotnet_sha1_new sha1_new = NativeAPI.Bind<aws_dotnet_sha1_new>();
            public static aws_dotnet_sha256_new sha256_new = NativeAPI.Bind<aws_dotnet_sha256_new>();
            public static aws_dotnet_md5_new md5_new = NativeAPI.Bind<aws_dotnet_md5_new>();
            public static aws_dotnet_hash_update update = NativeAPI.Bind<aws_dotnet_hash_update>();
            public static aws_dotnet_hash_digest digest = NativeAPI.Bind<aws_dotnet_hash_digest>();
            public static aws_dotnet_hash_destroy destroy = NativeAPI.Bind<aws_dotnet_hash_destroy>();

        }
        private static LibraryHandle library = new LibraryHandle();

        private Handle hash;
        private uint length;

        public class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        private Hash(Handle hash, uint length)
        {
            this.hash = hash;
            this.length = length;
        }
        public static Hash sha1()
        {
            return new Hash(API.sha1_new(), 20);
        }
        public static Hash sha256()
        {
            return new Hash(API.sha256_new(), 32);
        }
        public static Hash md5()
        {
            return new Hash(API.md5_new(), 16);
        }

        public void update(byte[] buffer)
        {
            API.update(this.hash.DangerousGetHandle(), buffer, (uint)buffer.Length);
        }
        public byte[] digest(uint truncateTo = 0)
        {
            byte[] buffer = new byte[this.length];
            API.digest(this.hash.DangerousGetHandle(), truncateTo, buffer, this.length);
            return buffer;
        }
    }
}