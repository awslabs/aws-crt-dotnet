/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.IO;

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