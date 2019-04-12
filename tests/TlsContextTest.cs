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
        public void MinimumTlsVersionTest()
        {
            var options = new TlsContextOptions();
            options.MinimumTlsVersion = TlsVersions.TLSv1_3;
            Assert.Equal(TlsVersions.TLSv1_3, options.MinimumTlsVersion);
        }

        [Fact]
        public void AlpnListTest()
        {
            var options = new TlsContextOptions();
            options.AlpnList = "h2;x-amazon-mqtt";
            Assert.Equal("h2;x-amazon-mqtt", options.AlpnList);
        }

        [Fact]
        public void MaxFragmentSizeTest()
        {
            var options = new TlsContextOptions();
            options.MaxFragmentSize = 16 * 1024;
            Assert.Equal<uint>(16 * 1024, options.MaxFragmentSize);
        }

        [Fact]
        public void VerifyPeerTest()
        {
            var options = new TlsContextOptions();
            options.VerifyPeer = true;
            Assert.True(options.VerifyPeer);
        }
    }
}