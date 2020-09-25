/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.Auth;
using Aws.Crt.Http;

namespace tests
{
    public class AuthTest
    {
        [Fact]
        public void Signing()
        {
            var config = new AwsSigningConfig();
            var request = new HttpRequest();
            request.Method = "GET";
            request.Uri = "www.google.com";

            AwsSigner.SignRequest(request, config);
        }
    }
}
