/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.IO;

namespace tests
{
    public class HostResolverTest : BaseTest
    {
        [Fact]
        public void HostResolverLifetime()
        {
            var elg = new EventLoopGroup(1);
            var hostResolver = new DefaultHostResolver(elg, 8);
            // When hostResolver goes out of scope, the native handle will be released
            // Then elg should be released
        }
    }
}
