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
    public class SigningTest : IDisposable
    {
        public void Dispose()
        {
            // This method will be called after each test case runs
            Console.WriteLine("Test case completed, performing cleanup...");

            // Add any cleanup code here
            // For example: release resources, reset static variables, etc.

            // You can add specific cleanup logic based on your requirements
            AwsSigner.CheckForLeak();
        }

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
            Assert.Single(signedRequest.Headers);
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

        /* Sourced from the post-x-www-form-urlencoded test case in aws-c-auth */
        [Fact]
        public void SignCanonicalRequestByHeadersV4a()
        {
            var config = BuildBaseSigningConfig();
            config.SignatureType = AwsSignatureType.CANONICAL_REQUEST_VIA_HEADERS;
            config.Algorithm = AwsSigningAlgorithm.SIGV4A;


            var canonicalRequest = String.Join("\n",
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

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignCanonicalRequest(canonicalRequest, config);
            byte[] signatureValue = result.Get().Signature;

            ASCIIEncoding ascii = new ASCIIEncoding();

            Assert.True(AwsSigner.VerifyV4aCanonicalSigning(canonicalRequest, config, ascii.GetString(signatureValue), "b6618f6a65740a99e650b33b6b4b5bd0d43b176d721a3edfea7e7d2d56d936b1", "865ed22a7eadc9c5cb9d2cbaca1b3699139fedc5043dc6661864218330c8e518"));
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
            Assert.DoesNotContain(authValue, "Skip");
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
            String crtErrorName = Aws.Crt.CRT.ErrorName(crtErrorCode);
            /* AWS_AUTH_SIGNING_ILLEGAL_REQUEST_HEADER */
            Assert.Equal("AWS_AUTH_SIGNING_ILLEGAL_REQUEST_HEADER", crtErrorName);
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
            String crtErrorName = Aws.Crt.CRT.ErrorName(crtErrorCode);
            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal("AWS_AUTH_SIGNING_INVALID_CONFIGURATION", crtErrorName);
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
            String crtErrorName = Aws.Crt.CRT.ErrorName(crtErrorCode);
            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal("AWS_AUTH_SIGNING_INVALID_CONFIGURATION", crtErrorName);
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
            String crtErrorName = Aws.Crt.CRT.ErrorName(crtErrorCode);
            /* AWS_AUTH_SIGNING_INVALID_CONFIGURATION */
            Assert.Equal("AWS_AUTH_SIGNING_INVALID_CONFIGURATION", crtErrorName);
        }

        /*
        * Chunked encoding signing based on https://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-streaming.html
        */
        private static string CHUNKED_ACCESS_KEY_ID = "AKIAIOSFODNN7EXAMPLE";
        private static string CHUNKED_SECRET_ACCESS_KEY = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        private static string CHUNKED_TEST_REGION= "us-east-1";
        private static string CHUNKED_TEST_SERVICE = "s3";
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

        private AwsSigningConfig createTrailingHeadersSigningConfig() {
            AwsSigningConfig config = new AwsSigningConfig();
            config.Algorithm = AwsSigningAlgorithm.SIGV4;
            config.SignatureType = AwsSignatureType.HTTP_REQUEST_TRAILING_HEADERS;
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

        private HttpRequest createChunkedTrailerTestRequest() {

            string uri = "https://s3.amazonaws.com/examplebucket/chunkObject.txt";

            HttpHeader[] requestHeaders =
                    new HttpHeader[]{
                            new HttpHeader("Host", "s3.amazonaws.com"),
                            new HttpHeader("x-amz-storage-class", "REDUCED_REDUNDANCY"),
                            new HttpHeader("Content-Encoding", "aws-chunked"),
                            new HttpHeader("x-amz-decoded-content-length", "66560"),
                            new HttpHeader("Content-Length", "66824"),
                            new HttpHeader("x-amz-trailer", "first,second,third")
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
        private static byte[] EXPECTED_TRAILING_HEADERS_SIGNATURE = ASCIIEncoding.ASCII.GetBytes("df5735bd9f3295cd9386572292562fefc93ba94e80a0a1ddcbd652c4e0a75e6c");

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
        private HttpHeader[] createTrailingHeaders() {

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("first", "1st"));
            headers.Add(new HttpHeader("second", "2nd"));
            headers.Add(new HttpHeader("third", "3rd"));

            return headers.ToArray();
        }

        private static String CHUNK_STS_PRE_SIGNATURE = "AWS4-ECDSA-P256-SHA256-PAYLOAD\n" + "20130524T000000Z\n"
            + "20130524/s3/aws4_request\n";

        private static String CHUNK1_STS_POST_SIGNATURE = "\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\n"
            + "bf718b6f653bebc184e1479f1935b8da974d701b893afcf49e701f3e2f9f9c5a";

        private static String CHUNK2_STS_POST_SIGNATURE = "\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\n"
            + "2edc986847e209b4016e141a6dc8716d3207350f416969382d431539bf292e4a";

        private static String CHUNK3_STS_POST_SIGNATURE = "\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\n"
            + "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        private static String TRAILING_HEADERS_STS_PRE_SIGNATURE = "AWS4-ECDSA-P256-SHA256-TRAILER\n" + "20130524T000000Z\n"
            + "20130524/s3/aws4_request\n";

        private static String TRAILING_HEADERS_STS_POST_SIGNATURE = "\n83d8f190334fb741bc8daf73c891689d320bd8017756bc730c540021ed48001f";

        private static String CHUNKED_SIGV4A_CANONICAL_REQUEST = String.Join("\n",
            "PUT",
            "/examplebucket/chunkObject.txt",
            "",
            "content-encoding:aws-chunked",
            "content-length:66824",
            "host:s3.amazonaws.com",
            "x-amz-content-sha256:STREAMING-AWS4-ECDSA-P256-SHA256-PAYLOAD",
            "x-amz-date:20130524T000000Z",
            "x-amz-decoded-content-length:66560",
            "x-amz-region-set:us-east-1",
            "x-amz-storage-class:REDUCED_REDUNDANCY",
            "",
            "content-encoding;content-length;host;x-amz-content-sha256;x-amz-date;x-amz-decoded-content-length;x-amz-region-set;x-amz-storage-class",
            "STREAMING-AWS4-ECDSA-P256-SHA256-PAYLOAD");

        private static String CHUNKED_TRAILER_SIGV4A_CANONICAL_REQUEST = String.Join("\n",
            "PUT",
            "/examplebucket/chunkObject.txt",
            "",
            "content-encoding:aws-chunked",
            "content-length:66824",
            "host:s3.amazonaws.com",
            "x-amz-content-sha256:STREAMING-AWS4-ECDSA-P256-SHA256-PAYLOAD-TRAILER",
            "x-amz-date:20130524T000000Z",
            "x-amz-decoded-content-length:66560",
            "x-amz-region-set:us-east-1",
            "x-amz-storage-class:REDUCED_REDUNDANCY",
            "x-amz-trailer:first,second,third",
            "",
            "content-encoding;content-length;host;x-amz-content-sha256;x-amz-date;x-amz-decoded-content-length;x-amz-region-set;x-amz-storage-class;x-amz-trailer",
            "STREAMING-AWS4-ECDSA-P256-SHA256-PAYLOAD-TRAILER");

        private String buildTrailingHeadersStringToSign(byte[] previousSignature, String stsPostSignature) {
            StringBuilder stsBuilder = new StringBuilder();

            stsBuilder.Append(TRAILING_HEADERS_STS_PRE_SIGNATURE);
            String signature = System.Text.Encoding.UTF8.GetString(previousSignature);
            int paddingIndex = signature.IndexOf('*');
            if (paddingIndex != -1) {
                signature = signature.Substring(0, paddingIndex);
            }
            stsBuilder.Append(signature);
            stsBuilder.Append(stsPostSignature);

            return stsBuilder.ToString();
        }
        private String buildChunkStringToSign(byte[] previousSignature, String stsPostSignature) {
            StringBuilder stsBuilder = new StringBuilder();
            stsBuilder.Append(CHUNK_STS_PRE_SIGNATURE);
            String signature = System.Text.Encoding.UTF8.GetString(previousSignature);
            int paddingIndex = signature.IndexOf('*');
            if (paddingIndex != -1) {
                signature = signature.Substring(0, paddingIndex);
            }
            stsBuilder.Append(signature);
            stsBuilder.Append(stsPostSignature);

            return stsBuilder.ToString();
        }

        [Fact]
        public void SignTrailingHeadersSigv4()
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

            HttpHeader[] trailingHeaders = createTrailingHeaders();
            CrtResult<AwsSigner.CrtSigningResult> trailingHeadersResult = AwsSigner.SignTrailingHeaders(trailingHeaders, chunkSignature, createTrailingHeadersSigningConfig());
            chunkSignature = trailingHeadersResult.Get().Signature;
            Assert.True(chunkSignature.SequenceEqual(EXPECTED_TRAILING_HEADERS_SIGNATURE));
        }

           [Fact]
        public void SignChunkedSigv4a() {

            HttpRequest request = createChunkedTestRequest();
            AwsSigningConfig chunkedRequestSigningConfig = createChunkedRequestSigningConfig();
            chunkedRequestSigningConfig.Algorithm = AwsSigningAlgorithm.SIGV4A;
            chunkedRequestSigningConfig.SignedBodyValue = AwsSignedBodyValue.STREAMING_AWS4_ECDSA_P256_SHA256_PAYLOAD;

            CrtResult<AwsSigner.CrtSigningResult>  result = AwsSigner.SignHttpRequest(request, chunkedRequestSigningConfig);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;
            Assert.NotNull(signedRequest);

            ASCIIEncoding ascii = new ASCIIEncoding();

            byte[] requestSignature = signingResult.Signature;

            Stream chunk1 = createChunk1Stream();
            AwsSigningConfig chunkSigningConfig = createChunkSigningConfig();
            chunkSigningConfig.Algorithm = AwsSigningAlgorithm.SIGV4A;

            CrtResult<AwsSigner.CrtSigningResult> chunk1Result = AwsSigner.SignChunk(chunk1, requestSignature, chunkSigningConfig);

            String chunk1StringToSign = buildChunkStringToSign(requestSignature, CHUNK1_STS_POST_SIGNATURE);
            byte[] chunkSignature = chunk1Result.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(chunk1StringToSign, chunkSignature, VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));

            Stream chunk2 = createChunk2Stream();
            CrtResult<AwsSigner.CrtSigningResult> chunk2Result = AwsSigner.SignChunk(chunk2, chunkSignature, chunkSigningConfig);

            String chunk2StringToSign = buildChunkStringToSign(chunkSignature, CHUNK2_STS_POST_SIGNATURE);
            chunkSignature = chunk2Result.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(chunk2StringToSign, chunkSignature, VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));

            CrtResult<AwsSigner.CrtSigningResult> finalChunkResult = AwsSigner.SignChunk(null, chunkSignature, chunkSigningConfig);

            String finalChunkStringToSign = buildChunkStringToSign(chunkSignature, CHUNK3_STS_POST_SIGNATURE);
            chunkSignature = finalChunkResult.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(finalChunkStringToSign, chunkSignature, VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));
    }

        [Fact]
        public void SignTrailingHeadersSigv4a()
        {
            HttpRequest request = createChunkedTrailerTestRequest();
            AwsSigningConfig chunkedRequestSigningConfig = createChunkedRequestSigningConfig();
            AwsSigningConfig chunkSigningConfig = createChunkSigningConfig();
            AwsSigningConfig trailingHeadersSigningConfig = createTrailingHeadersSigningConfig();
            chunkedRequestSigningConfig.SignedBodyValue = AwsSignedBodyValue.STREAMING_AWS4_ECDSA_P256_SHA256_PAYLOAD_TRAILER;
            chunkedRequestSigningConfig.Algorithm = AwsSigningAlgorithm.SIGV4A;
            chunkSigningConfig.Algorithm = AwsSigningAlgorithm.SIGV4A;
            trailingHeadersSigningConfig.Algorithm = AwsSigningAlgorithm.SIGV4A;

            CrtResult<AwsSigner.CrtSigningResult> result = AwsSigner.SignHttpRequest(request, chunkedRequestSigningConfig);
            AwsSigner.CrtSigningResult signingResult = result.Get();
            HttpRequest signedRequest = signingResult.SignedRequest;
            Assert.NotNull(signedRequest);

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] requestSignature = signingResult.Signature;

            Stream chunk1 = createChunk1Stream();
            CrtResult<AwsSigner.CrtSigningResult> chunk1Result = AwsSigner.SignChunk(chunk1, requestSignature, chunkSigningConfig);
            String chunk1StringToSign = buildChunkStringToSign(requestSignature, CHUNK1_STS_POST_SIGNATURE);
            byte[] chunkSignature = chunk1Result.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(chunk1StringToSign, chunkSignature,
                VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));

            Stream chunk2 = createChunk2Stream();
            CrtResult<AwsSigner.CrtSigningResult> chunk2Result = AwsSigner.SignChunk(chunk2, chunkSignature, chunkSigningConfig);
            String chunk2StringToSign = buildChunkStringToSign(chunkSignature, CHUNK2_STS_POST_SIGNATURE);
            chunkSignature = chunk2Result.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(chunk2StringToSign, chunkSignature,
                VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));

            CrtResult<AwsSigner.CrtSigningResult> finalChunkResult = AwsSigner.SignChunk(null, chunkSignature, chunkSigningConfig);
            String finalChunkStringToSign = buildChunkStringToSign(chunkSignature, CHUNK3_STS_POST_SIGNATURE);
            chunkSignature = finalChunkResult.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(finalChunkStringToSign, chunkSignature,
                VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));

            HttpHeader[] trailingHeaders = createTrailingHeaders();
            CrtResult<AwsSigner.CrtSigningResult> trailingHeadersResult = AwsSigner.SignTrailingHeaders(trailingHeaders, chunkSignature, trailingHeadersSigningConfig);
            String trailingHeadersStringToSign = buildTrailingHeadersStringToSign(chunkSignature, TRAILING_HEADERS_STS_POST_SIGNATURE);
            chunkSignature = trailingHeadersResult.Get().Signature;
            Assert.True(AwsSigner.VerifyV4aSignature(trailingHeadersStringToSign, chunkSignature,
                VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));
        }

        private static string VERIFIER_TEST_ECC_PUB_X = "18b7d04643359f6ec270dcbab8dce6d169d66ddc9778c75cfb08dfdb701637ab";
        private static string VERIFIER_TEST_ECC_PUB_Y = "fa36b35e4fe67e3112261d2e17a956ef85b06e44712d2850bcd3c2161e9993f2";
        private static string VERIFIER_TEST_STRING_TO_SIGN = "AWS4-ECDSA-P256-SHA256-PAYLOAD\n20130524T000000Z\n20130524/s3/aws4_request\n3044022023cfe85576d032aa102003a8f0397a79e95c653948e5ddc14aef866d06d4bb90022070c3ce70537a3e8d67237ada26044b990cb61d3443bdb453d479a7ec5b9b7a84\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\nbf718b6f653bebc184e1479f1935b8da974d701b893afcf49e701f3e2f9f9c5a";
        private static byte[] VERIFIER_SIGNATURE = ASCIIEncoding.ASCII.GetBytes("3045022062ee82ba2ef3cf7661494e4bd3f0eabaf3154dcdccb29bb811d66db0b986eb44022100977cb9a3082fa9d287ba03d4c52652904da263047a5e12e901617663d24baba5");

        [Fact]
        public void CheckSigv4aSignatureValueVerifier()
        {
            Assert.True(AwsSigner.VerifyV4aSignature(VERIFIER_TEST_STRING_TO_SIGN, VERIFIER_SIGNATURE, VERIFIER_TEST_ECC_PUB_X, VERIFIER_TEST_ECC_PUB_Y));
        }
    }
}
