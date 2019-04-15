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
    public class TlsContextOptionsTest
    {
        [Fact]
        public void ClientMtlsTest()
        {
            var options = TlsContextOptions.ClientMtlsFromPath(
                "/Users/boswej/Downloads/d97cec9e7f-certificate.pem.crt", 
                "/Users/boswej/Downloads/d97cec9e7f-private.pem.key");
            var tls = new ClientTlsContext(options);
        }

        [Fact]
        public void ServerMtlsTest()
        {
            var options = TlsContextOptions.ClientMtlsFromPath(
                "/Users/boswej/Downloads/d97cec9e7f-certificate.pem.crt",
                "/Users/boswej/Downloads/d97cec9e7f-private.pem.key");
            var tls = new ServerTlsContext(options);
        }
    }
}