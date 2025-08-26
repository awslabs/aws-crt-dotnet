/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.IO;

namespace tests
{
    public class  SocketOptionsTest : BaseTest
    {
        [Fact]
        public void SocketOptionsFields()
        {
            var options = new SocketOptions();
            options.Domain = SocketDomain.IPv4;
            options.Type = SocketType.Stream;
            options.ConnectTimeoutMs = 42;
            options.KeepAliveIntervalSeconds = 6;
            options.KeepAliveTimeoutSeconds = 12;
            options.KeepAlive = true;

            SocketOptions.Handle nativeOptions = options.NativeHandle;
            Assert.False(nativeOptions.IsInvalid);
        }
    }
}
