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

namespace tests
{
    public class HostResolverTest
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
