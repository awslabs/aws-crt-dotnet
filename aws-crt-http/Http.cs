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
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt.IO;

namespace Aws.Crt.Http
{
    public static class Http
    {
        public delegate string aws_dotnet_http_status_text(int statusCode);
        public static aws_dotnet_http_status_text GetStatusText = NativeAPI.Bind<aws_dotnet_http_status_text>();
    }
}