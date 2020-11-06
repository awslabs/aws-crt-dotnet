/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

using Aws.Crt;
using Aws.Crt.Auth;
using Aws.Crt.Http;
using Aws.Crt.IO;

namespace tests
{
    public class SigningTest
    {

        private static string GetHeaderValue(HttpRequest request, String name) {
            foreach (HttpHeader header in request.Headers) {
                if (name == header.Name) {
                    return header.Value;
                }
            }

            return null;
        }
        private static bool HasHeader(HttpRequest request, String name, String value) {
            string headerValue = GetHeaderValue(request, name);
            if (headerValue == null) {
                return false;
            }

            return value == null || value == headerValue;
        }

        private static bool ShouldSignHeader(byte[] headerName, uint length) {
            byte[] skipHeader = ASCIIEncoding.ASCII.GetBytes("Skip");
            return !skipHeader.SequenceEqual(headerName);
        }

        private AwsSigningConfig BuildBaseSigningConfig() {
            DateTime date = new DateTime(2015, 8, 30, 12, 36, 0, DateTimeKind.Utc);

            var credentials = new Credentials("AKIDEXAMPLE", "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY", null);

            var config = new AwsSigningConfig();
            config.Credentials = credentials;
            config.Timestamp = new DateTimeOffset(date);
            config.Region = "us-east-1";
            config.Service = "service";

            return config;
        }

        private HttpRequest BuildTestSuiteRequestWithoutBody() {
            var request = new HttpRequest();
            request.Method = "GET";
            request.Uri = "/?Param-3=Value3&Param=Value2&%E1%88%B4=Value1";

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("Host", "example.amazonaws.com"));

            request.Headers = headers.ToArray();

            return request;
        }

        private HttpRequest BuildTestSuiteRequestWithBody() {
            var request = new HttpRequest();
            request.Method = "POST";
            request.Uri = "/";
            request.BodyStream = new MemoryStream(ASCIIEncoding.ASCII.GetBytes("Param1=value1"));

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("Host", "example.amazonaws.com"));
            headers.Add(new HttpHeader("Content-Length", "13"));
            headers.Add(new HttpHeader("Content-Type", "application/x-www-form-urlencoded"));

            request.Headers = headers.ToArray();

            return request;
        }

        private HttpRequest BuildRequestWithIllegalHeader() {
            var request = new HttpRequest();
            request.Method = "GET";
            request.Uri = "/";

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("Host", "example.amazonaws.com"));
            headers.Add(new HttpHeader("Authorization", "bad"));

            request.Headers = headers.ToArray();

            return request;
        }

        private HttpRequest BuildRequestWithSkippedHeader() {
            var request = new HttpRequest();
            request.Method = "GET";
            request.Uri = "/";

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("Host", "example.amazonaws.com"));
            headers.Add(new HttpHeader("Skip", "me"));

            request.Headers = headers.ToArray();

            return request;
        }

        /* Sourced from the get-vanilla-query-order-encoded test case in aws-c-auth */
        [Fact]
        public void SignBodylessRequestByHeaders()
        {
            var config = BuildBaseSigningConfig();
            var request = BuildTestSuiteRequestWithoutBody();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);
            HttpRequest signedRequest = result.Get();

            Assert.Equal("GET", signedRequest.Method);
            Assert.Equal("/?Param-3=Value3&Param=Value2&%E1%88%B4=Value1", signedRequest.Uri);
            Assert.Equal(3, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));
            Assert.True(HasHeader(signedRequest, "X-Amz-Date", "20150830T123600Z"));
            Assert.True(HasHeader(signedRequest, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, SignedHeaders=host;x-amz-date, Signature=371d3713e185cc334048618a97f809c9ffe339c62934c032af5a0e595648fcac"));
        }

        /* Sourced from the get-vanilla-query-order-encoded test case in aws-c-auth */
        [Fact]
        public void SignBodylessRequestByQuery()
        {
            var config = BuildBaseSigningConfig();
            config.SignatureType = AwsSignatureType.HTTP_REQUEST_VIA_QUERY_PARAMS;
            config.ExpirationInSeconds = 3600;

            var request = BuildTestSuiteRequestWithoutBody();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);
            HttpRequest signedRequest = result.Get();

            Assert.Equal("GET", signedRequest.Method);
            Assert.Equal("/?Param-3=Value3&Param=Value2&%E1%88%B4=Value1&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIDEXAMPLE%2F20150830%2Fus-east-1%2Fservice%2Faws4_request&X-Amz-Date=20150830T123600Z&X-Amz-SignedHeaders=host&X-Amz-Expires=3600&X-Amz-Signature=c5f1848ceec943ac2ca68ee720460c23aaae30a2300586597ada94c4a65e4787", signedRequest.Uri);
            Assert.Equal(1, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));
        }

        /* Sourced from the post-x-www-form-urlencoded test case in aws-c-auth */
        [Fact]
        public void SignBodyRequestByHeaders()
        {
            var config = BuildBaseSigningConfig();
            config.SignedBodyHeader = AwsSignedBodyHeaderType.X_AMZ_CONTENT_SHA256;

            var request = BuildTestSuiteRequestWithBody();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);
            HttpRequest signedRequest = result.Get();

            Assert.Equal("POST", signedRequest.Method);
            Assert.Equal("/", signedRequest.Uri);
            Assert.Equal(6, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));
            Assert.True(HasHeader(signedRequest, "X-Amz-Date", "20150830T123600Z"));
            Assert.True(HasHeader(signedRequest, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, SignedHeaders=content-length;content-type;host;x-amz-content-sha256;x-amz-date, Signature=d3875051da38690788ef43de4db0d8f280229d82040bfac253562e56c3f20e0b"));
            Assert.True(HasHeader(signedRequest, "Content-Type", "application/x-www-form-urlencoded"));
            Assert.True(HasHeader(signedRequest, "x-amz-content-sha256", "9095672bbd1f56dfc5b65f3e153adc8731a4a654192329106275f4c7b24d0b6e"));
            Assert.True(HasHeader(signedRequest, "Content-Length", "13"));
        }      

        /* Sourced from the post-x-www-form-urlencoded test case in aws-c-auth */
        [Fact]
        public void SignCanonicalRequestByHeaders()
        {
            var config = BuildBaseSigningConfig();
            config.SignedBodyHeader = AwsSignedBodyHeaderType.X_AMZ_CONTENT_SHA256;
            config.SignatureType = AwsSignatureType.CANONICAL_REQUEST_VIA_HEADERS;


            var canonicalRequest = String.Join("\n",
                "POST",
                "/",
                "",
                "content-length:13",
                "content-type:application/x-www-form-urlencoded",
                "host:example.amazonaws.com",
                "x-amz-content-sha256:9095672bbd1f56dfc5b65f3e153adc8731a4a654192329106275f4c7b24d0b6e",
                "x-amz-date:20150830T123600Z",
                "",
                "content-length;content-type;host;x-amz-content-sha256;x-amz-date",
                "9095672bbd1f56dfc5b65f3e153adc8731a4a654192329106275f4c7b24d0b6e");

            CrtResult<String> result = AwsSigner.SignCanonicalRequest(canonicalRequest, config);
            String signatureValue = result.Get();

            Assert.Equal("d3875051da38690788ef43de4db0d8f280229d82040bfac253562e56c3f20e0b", signatureValue);
        } 

        /* Sourced from the post-x-www-form-urlencoded test case in aws-c-auth */
        [Fact]
        public void SignCanonicalRequestByHeadersV4a()
        {
            var config = BuildBaseSigningConfig();
            config.SignatureType = AwsSignatureType.CANONICAL_REQUEST_VIA_HEADERS;
            config.Algorithm = AwsSigningAlgorithm.SIGV4A;


            var canonicalRequest = String.Join('\n',
                "POST",
                "/",
                "",
                "content-length:13",
                "content-type:application/x-www-form-urlencoded",
                "host:example.amazonaws.com",
                "x-amz-content-sha256:9095672bbd1f56dfc5b65f3e153adc8731a4a654192329106275f4c7b24d0b6e",
                "x-amz-date:20150830T123600Z",
                "x-amz-region-set:us-east-1",
                "",
                "content-length;content-type;host;x-amz-content-sha256;x-amz-date;x-amz-region-set",
                "9095672bbd1f56dfc5b65f3e153adc8731a4a654192329106275f4c7b24d0b6e");

            CrtResult<String> result = AwsSigner.SignCanonicalRequest(canonicalRequest, config);
            String signatureValue = result.Get();

            Assert.True(AwsSigner.VerifyV4aCanonicalSigning(canonicalRequest, config, signatureValue, "b6618f6a65740a99e650b33b6b4b5bd0d43b176d721a3edfea7e7d2d56d936b1", "865ed22a7eadc9c5cb9d2cbaca1b3699139fedc5043dc6661864218330c8e518"));
        } 


        [Fact]
        public void SignRequestByHeadersWithHeaderSkip()
        {
            var config = BuildBaseSigningConfig();
            config.ShouldSignHeader = ShouldSignHeader;

            var request = BuildRequestWithSkippedHeader();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);
            HttpRequest signedRequest = result.Get();

            Assert.Equal("GET", signedRequest.Method);
            Assert.Equal("/", signedRequest.Uri);
            Assert.Equal(4, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));
            Assert.True(HasHeader(signedRequest, "X-Amz-Date", "20150830T123600Z"));
            Assert.True(HasHeader(signedRequest, "Skip", "me"));

            /* Verify Skip is not in the signed headers component of the Authorization header */
            String authValue = GetHeaderValue(signedRequest, "Authorization");
            Assert.Equal(false, authValue.Contains("Skip"));
        }    

        [Fact]
        public void SignRequestFailureIllegalHeader()
        {
            var config = BuildBaseSigningConfig();

            var request = BuildRequestWithIllegalHeader();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                HttpRequest req = result.Get();
            } catch (CrtException e) {
                crtErrorCode = e.ErrorCode;
            }

            /* AWS_AUTH_SIGNING_ILLEGAL_REQUEST_HEADER */
            Assert.Equal(1024 * 6 + 6, crtErrorCode);
        }   

        [Fact]
        public void SignRequestFailureNoService()
        {
            var config = BuildBaseSigningConfig();
            config.Service = null;

            var request = BuildTestSuiteRequestWithoutBody();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                HttpRequest req = result.Get();
            } catch (CrtException e) {
                crtErrorCode = e.ErrorCode;
            }

            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal(1024 * 6 + 7, crtErrorCode);
        }   

        [Fact]
        public void SignRequestFailureNoRegion()
        {
            var config = BuildBaseSigningConfig();
            config.Region = null;

            var request = BuildTestSuiteRequestWithoutBody();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                HttpRequest req = result.Get();
            } catch (CrtException e) {
                crtErrorCode = e.ErrorCode;
            }

            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal(1024 * 6 + 7, crtErrorCode);
        }

        [Fact]
        public void SignRequestFailureNoCredentials()
        {
            var config = BuildBaseSigningConfig();
            config.Credentials = null;

            var request = BuildTestSuiteRequestWithoutBody();

            CrtResult<HttpRequest> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                HttpRequest req = result.Get();
            } catch (CrtException e) {
                crtErrorCode = e.ErrorCode;
            }

            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal(1024 * 6 + 7, crtErrorCode);
        }           
    }
}
