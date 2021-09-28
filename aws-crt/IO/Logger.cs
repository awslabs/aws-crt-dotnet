/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt;

namespace Aws.Crt.IO 
{
    public enum LogLevel 
    {
        NONE = 0,
        FATAL = 1,
        ERROR = 2,
        WARN = 3,
        INFO = 4,
        DEBUG = 5,
        TRACE = 6,
    }

    public sealed class Logger 
    {
        [SecuritySafeCritical]
        internal static class API
        {
            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            public delegate void aws_dotnet_logger_enable(int level, string filename);

            public static aws_dotnet_logger_enable enable = NativeAPI.Bind<aws_dotnet_logger_enable>();
        }

        public static void EnableLogging(LogLevel level, string filename = null)
        {
            API.enable((int)level, filename);
        }
    }
}
