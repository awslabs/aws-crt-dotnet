/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Runtime.InteropServices;

namespace Aws.Crt.Checksums
{
    public class Crc
    {
        internal static class API
        {
            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            public delegate UInt32 aws_dotnet_crc32([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.U1)] byte[] buffer,
                                                   Int32 length, UInt32 previous);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            public delegate UInt32 aws_dotnet_crc32c([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.U1)] byte[] buffer,
                                                   Int32 length, UInt32 previous);

            public static aws_dotnet_crc32 crc32 = NativeAPI.Bind<aws_dotnet_crc32>();
            public static aws_dotnet_crc32c crc32c = NativeAPI.Bind<aws_dotnet_crc32c>();
        }
        public static uint crc32(byte[] buffer, uint previous = 0)
        {
            return API.crc32(buffer, buffer.Length, previous);
        }
        public static uint crc32c(byte[] buffer, uint previous = 0)
        {
            return API.crc32c(buffer, buffer.Length, previous);
        }
    }
}
