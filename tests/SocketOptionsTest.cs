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
using Xunit;

using Aws.CRT.IO;

namespace tests
{
    public class  SocketOptionsTest
    {
        [Fact]
        public void SocketOptionsFields()
        {
            SocketOptions options = new SocketOptions();
            options.Domain = SocketDomain.IPv4;
            options.Type = SocketType.STREAM;
            options.ConnectTimeoutMs = 42;
            options.KeepAliveIntervalSeconds = 6;
            options.KeepAliveTimeoutSeconds = 12;
            options.KeepAlive = true;
        }
    }
}