/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using Aws.Crt.Auth;
using Aws.Crt.Http;

namespace tests
{
    public class AuthTest
    {
        private static bool HasHeader(HttpRequest request, String name, String value) {
            foreach (HttpHeader header in request.Headers) {
                if (name == header.Name) {
                    if (value == null || value == header.Value) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ShouldSignHeader(byte[] headerName, uint length) {
            byte[] skipHeader = ASCIIEncoding.ASCII.GetBytes("Skip");
            return !skipHeader.SequenceEqual(headerName);
        }

        [Fact]
        public void SimpleRequestSign()
        {
            DateTime date = new DateTime(2015, 8, 30, 12, 36, 0, DateTimeKind.Utc);

            var credentials = new Credentials("AccessKey", "SecretStuff", null);

            var config = new AwsSigningConfig();
            config.Region = "FakeRegion";
            config.Service = "FakeService";
            config.Credentials = credentials;
            config.Timestamp = new DateTimeOffset(date);
            config.ShouldSignHeader = AuthTest.ShouldSignHeader;

            var request = new HttpRequest();
            request.Method = "GET";
            request.Uri = "www.google.com";
            request.BodyStream = new MemoryStream(ASCIIEncoding.ASCII.GetBytes("DerpDerp"));

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("Host", "testing.example.com"));
            headers.Add(new HttpHeader("Skip", "Me"));

            request.Headers = headers.ToArray();

            Task<HttpRequest> result = AwsSigner.SignRequest(request, config);
            HttpRequest signedRequest = result.Result;

            Assert.Equal("GET", signedRequest.Method);
            Assert.Equal(4, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "testing.example.com"));
            Assert.True(HasHeader(signedRequest, "X-Amz-Date", "20150830T123600Z"));
            Assert.True(HasHeader(signedRequest, "Authorization", null));
        }
    }
}
