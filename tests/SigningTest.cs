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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;

            Assert.Equal("GET", signedRequest.Method);
            Assert.Equal("/?Param-3=Value3&Param=Value2&%E1%88%B4=Value1", signedRequest.Uri);
            Assert.Equal(3, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));
            Assert.True(HasHeader(signedRequest, "X-Amz-Date", "20150830T123600Z"));
            Assert.True(HasHeader(signedRequest, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, SignedHeaders=host;x-amz-date, Signature=371d3713e185cc334048618a97f809c9ffe339c62934c032af5a0e595648fcac"));

            byte[] signature = signingResult.Signature;
            Assert.True(signature.SequenceEqual(ASCIIEncoding.ASCII.GetBytes("371d3713e185cc334048618a97f809c9ffe339c62934c032af5a0e595648fcac")));
        }

        /* Sourced from the get-vanilla-query-order-encoded test case in aws-c-auth */
        [Fact]
        public void SignBodylessRequestByQuery()
        {
            var config = BuildBaseSigningConfig();
            config.SignatureType = AwsSignatureType.HTTP_REQUEST_VIA_QUERY_PARAMS;
            config.ExpirationInSeconds = 3600;

            var request = BuildTestSuiteRequestWithoutBody();

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;

            Assert.Equal("GET", signedRequest.Method);
            Assert.Equal("/?Param-3=Value3&Param=Value2&%E1%88%B4=Value1&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIDEXAMPLE%2F20150830%2Fus-east-1%2Fservice%2Faws4_request&X-Amz-Date=20150830T123600Z&X-Amz-SignedHeaders=host&X-Amz-Expires=3600&X-Amz-Signature=c5f1848ceec943ac2ca68ee720460c23aaae30a2300586597ada94c4a65e4787", signedRequest.Uri);
            Assert.Equal(1, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));

            byte[] signature = signingResult.Signature;
            Assert.True(signature.SequenceEqual(ASCIIEncoding.ASCII.GetBytes("c5f1848ceec943ac2ca68ee720460c23aaae30a2300586597ada94c4a65e4787")));
        }

        /* Sourced from the post-x-www-form-urlencoded test case in aws-c-auth */
        [Fact]
        public void SignBodyRequestByHeaders()
        {
            var config = BuildBaseSigningConfig();
            config.SignedBodyHeader = AwsSignedBodyHeaderType.X_AMZ_CONTENT_SHA256;

            var request = BuildTestSuiteRequestWithBody();

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;

            Assert.Equal("POST", signedRequest.Method);
            Assert.Equal("/", signedRequest.Uri);
            Assert.Equal(6, signedRequest.Headers.Length);
            Assert.True(HasHeader(signedRequest, "Host", "example.amazonaws.com"));
            Assert.True(HasHeader(signedRequest, "X-Amz-Date", "20150830T123600Z"));
            Assert.True(HasHeader(signedRequest, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, SignedHeaders=content-length;content-type;host;x-amz-content-sha256;x-amz-date, Signature=d3875051da38690788ef43de4db0d8f280229d82040bfac253562e56c3f20e0b"));
            Assert.True(HasHeader(signedRequest, "Content-Type", "application/x-www-form-urlencoded"));
            Assert.True(HasHeader(signedRequest, "x-amz-content-sha256", "9095672bbd1f56dfc5b65f3e153adc8731a4a654192329106275f4c7b24d0b6e"));
            Assert.True(HasHeader(signedRequest, "Content-Length", "13"));

            byte[] signature = signingResult.Signature;
            Assert.True(signature.SequenceEqual(ASCIIEncoding.ASCII.GetBytes("d3875051da38690788ef43de4db0d8f280229d82040bfac253562e56c3f20e0b")));
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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignCanonicalRequest(canonicalRequest, config);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            byte[] signature = signingResult.Signature;

            Assert.True(signature.SequenceEqual(ASCIIEncoding.ASCII.GetBytes("d3875051da38690788ef43de4db0d8f280229d82040bfac253562e56c3f20e0b")));
        } 

        [Fact]
        public void SignRequestByHeadersWithHeaderSkip()
        {
            var config = BuildBaseSigningConfig();
            config.ShouldSignHeader = ShouldSignHeader;

            var request = BuildRequestWithSkippedHeader();

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;

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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                AwsSigner.CrtSigningResult signingResult = result.Get();
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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                AwsSigner.CrtSigningResult signingResult = result.Get();
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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                AwsSigner.CrtSigningResult signingResult = result.Get();
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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, config);

            Assert.Throws<CrtException>(() => result.Get());

            int crtErrorCode = 0;
            try {
                AwsSigner.CrtSigningResult signingResult = result.Get();
            } catch (CrtException e) {
                crtErrorCode = e.ErrorCode;
            }

            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal(1024 * 6 + 7, crtErrorCode);
        }

        /*
        * Chunked encoding signing based on https://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-streaming.html
        */
        private static string CHUNKED_ACCESS_KEY_ID = "AKIAIOSFODNN7EXAMPLE";
        private static string CHUNKED_SECRET_ACCESS_KEY = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        private static string CHUNKED_TEST_REGION= "us-east-1";
        private static string CHUNKED_TEST_SERVICE = "s3";
        private static string CHUNKED_TEST_SIGNING_TIME = "2013-05-24T00:00:00Z";
        private static DateTime CHUNKED_SIGNING_DATE = new DateTime(2013, 5, 24, 0, 0, 0, DateTimeKind.Utc);
        private static int CHUNK1_SIZE = 65536;
        private static int CHUNK2_SIZE = 1024;

        private Credentials createChunkedTestCredentials() {
            return new Credentials(CHUNKED_ACCESS_KEY_ID, CHUNKED_SECRET_ACCESS_KEY, null);
        }

        private AwsSigningConfig createChunkedRequestSigningConfig() {
            AwsSigningConfig config = new AwsSigningConfig();
            config.Algorithm = AwsSigningAlgorithm.SIGV4;
            config.SignatureType = AwsSignatureType.HTTP_REQUEST_VIA_HEADERS;
            config.Region = CHUNKED_TEST_REGION;
            config.Service = CHUNKED_TEST_SERVICE;
            config.Timestamp = new DateTimeOffset(CHUNKED_SIGNING_DATE);
            config.UseDoubleUriEncode = false;
            config.ShouldNormalizeUriPath = true;
            config.SignedBodyHeader = AwsSignedBodyHeaderType.X_AMZ_CONTENT_SHA256;
            config.SignedBodyValue = AwsSignedBodyValue.STREAMING_AWS4_HMAC_SHA256_PAYLOAD;
            config.Credentials = createChunkedTestCredentials();

            return config;
        }

        private AwsSigningConfig createChunkSigningConfig() {
            AwsSigningConfig config = new AwsSigningConfig();
            config.Algorithm = AwsSigningAlgorithm.SIGV4;
            config.SignatureType = AwsSignatureType.HTTP_REQUEST_CHUNK;
            config.Region = CHUNKED_TEST_REGION;
            config.Service = CHUNKED_TEST_SERVICE;
            config.Timestamp = new DateTimeOffset(CHUNKED_SIGNING_DATE);
            config.UseDoubleUriEncode = false;
            config.ShouldNormalizeUriPath = true;
            config.SignedBodyHeader = AwsSignedBodyHeaderType.NONE;
            config.Credentials = createChunkedTestCredentials();

            return config;
        }

        private HttpRequest createChunkedTestRequest() {

            string uri = "https://s3.amazonaws.com/examplebucket/chunkObject.txt";

            HttpHeader[] requestHeaders =
                    new HttpHeader[]{
                            new HttpHeader("Host", "s3.amazonaws.com"),
                            new HttpHeader("x-amz-storage-class", "REDUCED_REDUNDANCY"),
                            new HttpHeader("Content-Encoding", "aws-chunked"),
                            new HttpHeader("x-amz-decoded-content-length", "66560"),
                            new HttpHeader("Content-Length", "66824")
                    };

            HttpRequest request = new HttpRequest();
            request.Uri = uri;
            request.Method = "PUT";
            request.Headers = requestHeaders;
            request.BodyStream = null;

            return request;
        }

        private Stream createChunk1Stream() {
            StringBuilder chunkBody = new StringBuilder();
            for (int i = 0; i < CHUNK1_SIZE; ++i) {
                chunkBody.Append('a');
            }

            return new MemoryStream(ASCIIEncoding.ASCII.GetBytes(chunkBody.ToString()));
        }

        private Stream createChunk2Stream() {
            StringBuilder chunkBody = new StringBuilder();
            for (int i = 0; i < CHUNK2_SIZE; ++i) {
                chunkBody.Append('a');
            }

            return new MemoryStream(ASCIIEncoding.ASCII.GetBytes(chunkBody.ToString()));
        }

        private static string EXPECTED_CHUNK_REQUEST_AUTHORIZATION_HEADER =
                "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request, " +
                "SignedHeaders=content-encoding;content-length;host;x-amz-content-sha256;x-amz-date;x-amz-decoded-content-length;x-" +
                "amz-storage-class, Signature=4f232c4386841ef735655705268965c44a0e4690baa4adea153f7db9fa80a0a9";

        private static byte[] EXPECTED_REQUEST_SIGNATURE = ASCIIEncoding.ASCII.GetBytes("4f232c4386841ef735655705268965c44a0e4690baa4adea153f7db9fa80a0a9");
        private static byte[] EXPECTED_FIRST_CHUNK_SIGNATURE = ASCIIEncoding.ASCII.GetBytes("ad80c730a21e5b8d04586a2213dd63b9a0e99e0e2307b0ade35a65485a288648");
        private static byte[] EXPECTED_SECOND_CHUNK_SIGNATURE = ASCIIEncoding.ASCII.GetBytes("0055627c9e194cb4542bae2aa5492e3c1575bbb81b612b7d234b86a503ef5497");
        private static byte[] EXPECTED_FINAL_CHUNK_SIGNATURE = ASCIIEncoding.ASCII.GetBytes("b6c6ea8a5354eaf15b3cb7646744f4275b71ea724fed81ceb9323e279d449df9");
       
        [Fact]
        public void SignChunkedRequest()
        {
            HttpRequest request = createChunkedTestRequest();

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, createChunkedRequestSigningConfig());
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;
            Assert.NotNull(signedRequest);
            Assert.Equal(GetHeaderValue(signedRequest, "Authorization"), EXPECTED_CHUNK_REQUEST_AUTHORIZATION_HEADER);

            byte[] requestSignature = signingResult.Signature;
            Assert.True(requestSignature.SequenceEqual(EXPECTED_REQUEST_SIGNATURE));

            Stream chunk1 = createChunk1Stream();
            CrtResult<AwsSigner.CrtSigningResult> chunk1Result = AwsSigner.SignChunk(chunk1, requestSignature, createChunkSigningConfig());

            byte[] chunkSignature = chunk1Result.Get().Signature;
            Assert.True(chunkSignature.SequenceEqual(EXPECTED_FIRST_CHUNK_SIGNATURE));

            Stream chunk2 = createChunk2Stream();
            CrtResult<AwsSigner.CrtSigningResult> chunk2Result = AwsSigner.SignChunk(chunk2, chunkSignature, createChunkSigningConfig());

            chunkSignature = chunk2Result.Get().Signature;
            Assert.True(chunkSignature.SequenceEqual(EXPECTED_SECOND_CHUNK_SIGNATURE));

            CrtResult<AwsSigner.CrtSigningResult> finalChunkResult = AwsSigner.SignChunk(null, chunkSignature, createChunkSigningConfig());
            chunkSignature = finalChunkResult.Get().Signature;
            Assert.True(chunkSignature.SequenceEqual(EXPECTED_FINAL_CHUNK_SIGNATURE));
        }        
    }
}
