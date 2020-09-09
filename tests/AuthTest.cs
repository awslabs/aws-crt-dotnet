/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.Auth;

namespace tests
{
    public class AuthTest
    {
        [Fact]
        public void AuthCall()
        {
            var elg = new AuthTest();
            // When elg goes out of scope, the native handle will be released
        }
    }
}
