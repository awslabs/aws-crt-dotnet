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

using Aws.Crt.IO;
using Aws.Crt.Http;

namespace tests
{
    public class HttpClientConnectionTest
    {
        [Fact]
        public void HttpClientConnectionLifetime()
        {
            var elg = new EventLoopGroup(1);
            var clientBootstrap = new ClientBootstrap(elg);
            var options = new HttpClientConnectionOptions();
            options.ClientBootstrap = clientBootstrap;
            options.HostName = "www.amazon.com";
            options.Port = 80;
            options.OnConnectionSetup = (int errorCode) =>
            {
                Console.WriteLine("CONNECTED");
            };
            options.OnConnectionShutdown = (int errorCode) =>
            {
                Console.WriteLine("DISCONNECTED");
            };
            var connection = new HttpClientConnection(options);
            // When connection goes out of scope, the native handle will be released
            // Then clientBootstrap should be released
            // Then elg should be released
        }
    }
}
