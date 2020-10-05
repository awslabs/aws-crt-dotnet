/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.IO;

namespace tests
{
    public class EventLoopGroupTest
    {
        [Fact]
        public void EventLoopGroupLifetime()
        {
            var elg = new EventLoopGroup(1);
            // When elg goes out of scope, the native handle will be released
        }
    }
}
