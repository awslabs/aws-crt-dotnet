/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.IO;

namespace tests
{
    public class ClientBootstrapTest
    {
        [Fact]
        public void ClientBootstrapLifetime()
        {
            var elg = new EventLoopGroup(1);
            var hostResolver = new DefaultHostResolver(elg);
            var bootstrap = new ClientBootstrap(elg, hostResolver);
            // When these go out of scope, the native handle will be released
        }
    }
}