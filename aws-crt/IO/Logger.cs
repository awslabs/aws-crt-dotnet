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
            public delegate void aws_dotnet_logger_enable(int level);

            public static aws_dotnet_logger_enable enable = NativeAPI.Bind<aws_dotnet_logger_enable>();
        }

        public static void EnableLogging(LogLevel level)
        {
            API.enable((int)level);
        }
    }
}
