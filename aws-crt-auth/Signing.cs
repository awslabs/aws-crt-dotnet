/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Aws.Crt.Http;
using Aws.Crt.IO;

namespace Aws.Crt.Auth
{
    public enum AwsSigningAlgorithm {
        SIGV4 = 0
    }

    public enum AwsSignatureType {
        HTTP_REQUEST_VIA_HEADERS = 0,
        HTTP_REQUEST_VIA_QUERY_PARAMS = 1,
        HTTP_REQUEST_CHUNK = 2,
        HTTP_REQUEST_EVENT = 3,
        CANONICAL_REQUEST_VIA_HEADERS = 4,
        CANONICAL_REQUEST_VIA_QUERY_PARAMS = 5,
    }

    public class AwsSignedBodyValue {
        public static string EMPTY_SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        public static string UNSIGNED_PAYLOAD = "UNSIGNED-PAYLOAD";
        public static string STREAMING_AWS4_HMAC_SHA256_PAYLOAD = "STREAMING-AWS4-HMAC-SHA256-PAYLOAD";
        public static string STREAMING_AWS4_HMAC_SHA256_EVENTS = "STREAMING-AWS4-HMAC-SHA256-EVENTS";
    }

    public enum AwsSignedBodyHeaderType {
        NONE = 0,
        X_AMZ_CONTENT_SHA256 = 1
    }

    public delegate bool ShouldSignHeaderCallback([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)]byte[] headerName, UInt32 length);

    public class AwsSigningConfig {

        public AwsSigningAlgorithm Algorithm { get; set; }
        public AwsSignatureType SignatureType { get; set; }
        public string Region { get; set; }
        public string Service { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Credentials Credentials { get; set; }
        public ShouldSignHeaderCallback ShouldSignHeader { get; set; }
        public bool UseDoubleUriEncode { get; set; }
        public bool ShouldNormalizeUriPath { get; set; }
        public bool OmitSessionToken { get; set; }
        public string SignedBodyValue { get; set; }
        public AwsSignedBodyHeaderType SignedBodyHeader { get; set; }
        public ulong ExpirationInSeconds { get; set; }

        public AwsSigningConfig() {
            Algorithm = AwsSigningAlgorithm.SIGV4;
            SignatureType = AwsSignatureType.HTTP_REQUEST_VIA_HEADERS;
            Timestamp = DateTimeOffset.Now;
            UseDoubleUriEncode = true;
            ShouldNormalizeUriPath = true;
            OmitSessionToken = false;
            SignedBodyHeader = AwsSignedBodyHeaderType.NONE;
            ExpirationInSeconds = 0;
        }
    }

    public class AwsSigner {

        /*
         * Decouples the public signing configuration from what we toss over the wall
         */
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        internal struct AwsSigningConfigNative
        {
            [MarshalAs(UnmanagedType.I4)]
            public AwsSigningAlgorithm Algorithm;

            [MarshalAs(UnmanagedType.I4)]
            public AwsSignatureType SignatureType;

            [MarshalAs(UnmanagedType.LPStr)]
            public string Region;

            [MarshalAs(UnmanagedType.LPStr)]
            public string Service;

            [MarshalAs(UnmanagedType.I8)]
            public long MillisecondsSinceEpoch;

            [MarshalAs(UnmanagedType.LPStr)]
            public string AccessKeyId;

            [MarshalAs(UnmanagedType.LPStr)]
            public string SecretAccessKey;

            [MarshalAs(UnmanagedType.LPStr)]
            public string SessionToken;

            public ShouldSignHeaderCallback ShouldSignHeader;

            [MarshalAs(UnmanagedType.U1)]
            public bool UseDoubleUriEncode;

            [MarshalAs(UnmanagedType.U1)]
            public bool ShouldNormalizeUriPath;

            [MarshalAs(UnmanagedType.U1)]
            public bool OmitSessionToken;

            [MarshalAs(UnmanagedType.LPStr)]
            public string SignedBodyValue;

            [MarshalAs(UnmanagedType.I4)]
            public AwsSignedBodyHeaderType SignedBodyHeader;

            [MarshalAs(UnmanagedType.U8)]
            public ulong ExpirationInSeconds;

            public AwsSigningConfigNative(AwsSigningConfig config) 
            {
                Algorithm = config.Algorithm;
                SignatureType = config.SignatureType;
                Region = config.Region;
                Service = config.Service;
                MillisecondsSinceEpoch = config.Timestamp.ToUnixTimeMilliseconds();

                Credentials creds = config.Credentials;
                if (creds != null) {
                    AccessKeyId = creds.AccessKeyId != null ? System.Text.Encoding.UTF8.GetString(creds.AccessKeyId) : null;
                    SecretAccessKey = creds.SecretAccessKey != null ? System.Text.Encoding.UTF8.GetString(creds.SecretAccessKey) : null;
                    SessionToken = creds.SessionToken != null ? System.Text.Encoding.UTF8.GetString(creds.SessionToken) : null;
                } else {
                    AccessKeyId = null;
                    SecretAccessKey = null;
                    SessionToken = null;
                }

                ShouldSignHeader = config.ShouldSignHeader;
                UseDoubleUriEncode = config.UseDoubleUriEncode;
                ShouldNormalizeUriPath = config.ShouldNormalizeUriPath;
                OmitSessionToken = config.OmitSessionToken;
                SignedBodyValue = config.SignedBodyValue;
                SignedBodyHeader = config.SignedBodyHeader;
                ExpirationInSeconds = config.ExpirationInSeconds;
            }
        }

        internal static class API
        {
            internal delegate void HttpRequestSigningCompleteCallback(
                UInt64 id, 
                Int32 errorCode, 
                [MarshalAs(UnmanagedType.LPStr)] string uri, 
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)] HttpHeader[] headers, 
                UInt32 count);

            internal delegate void AwsDotnetAuthSignHttpRequest(
                                    [MarshalAs(UnmanagedType.LPStr)] string method,
                                    [MarshalAs(UnmanagedType.LPStr)] string uri,
                                    [In] HttpHeader[] headers,
                                    UInt32 header_count,
                                    [In] CrtStreamWrapper.DelegateTable stream_delegate_table,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    HttpRequestSigningCompleteCallback completion_callback_delegate);

            internal delegate void CanonicalRequestSigningCompleteCallback(
                UInt64 id, 
                Int32 errorCode, 
                [MarshalAs(UnmanagedType.LPStr)] string authorizationValue);

            internal delegate void AwsDotnetAuthSignCanonicalRequest(
                                    [MarshalAs(UnmanagedType.LPStr)] string canonical_request,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    CanonicalRequestSigningCompleteCallback completion_callback_delegate);                                    

            public static AwsDotnetAuthSignHttpRequest SignRequestNative = NativeAPI.Bind<AwsDotnetAuthSignHttpRequest>("aws_dotnet_auth_sign_http_request");

            public static AwsDotnetAuthSignCanonicalRequest SignCanonicalRequestNative = NativeAPI.Bind<AwsDotnetAuthSignCanonicalRequest>("aws_dotnet_auth_sign_canonical_request");

            public static HttpRequestSigningCompleteCallback OnHttpRequestSigningComplete = AwsSigner.OnHttpRequestSigningComplete;

            public static CanonicalRequestSigningCompleteCallback OnCanonicalRequestSigningComplete = AwsSigner.OnCanonicalRequestSigningComplete;

            private static LibraryHandle library = new LibraryHandle();
        }

        private class HttpRequestSigningCallback
        {
            public HttpRequest OriginalRequest;
            public TaskCompletionSource<HttpRequest> TaskSource = new TaskCompletionSource<HttpRequest>();
            public CrtStreamWrapper BodyStream;
            public ShouldSignHeaderCallback ShouldSignHeader;
        }

        private class CanonicalRequestSigningCallback
        {
            public String OriginalCanonicalRequest;
            public TaskCompletionSource<String> TaskSource = new TaskCompletionSource<String>();
        }

        private static StrongReferenceVendor<HttpRequestSigningCallback> PendingHttpRequestSignings = new StrongReferenceVendor<HttpRequestSigningCallback>();
        private static StrongReferenceVendor<CanonicalRequestSigningCallback> PendingCanonicalRequestSignings = new StrongReferenceVendor<CanonicalRequestSigningCallback>();

        private static void OnHttpRequestSigningComplete(ulong id, int errorCode, string uri, HttpHeader[] headers, uint headerCount)
        {
            HttpRequestSigningCallback callback = PendingHttpRequestSignings.ReleaseStrongReference(id);
            if (callback == null) {
                return;
            }

            if (errorCode != 0)
            {
                callback.TaskSource.SetException(new CrtException(errorCode));
            }
            else
            {
                HttpRequest sourceRequest = callback.OriginalRequest;
                HttpRequest signedRequest = new HttpRequest();
                signedRequest.Method = sourceRequest.Method;
                signedRequest.Uri = uri;
                signedRequest.Headers = headers;
                signedRequest.BodyStream = sourceRequest.BodyStream;

                callback.TaskSource.SetResult(signedRequest);
            }
        }

        public static Task<HttpRequest> SignRequest(HttpRequest request, AwsSigningConfig signingConfig) 
        {
            if (request == null || signingConfig == null) {
                throw new CrtException("Null argument passed to SignRequest");
            }

            if (request.BodyStream != null) {
                if (!request.BodyStream.CanSeek) {
                    throw new CrtException("Http request payload stream must be seekable in order to be signed");
                }
            }

            var nativeConfig = new AwsSigningConfigNative(signingConfig);

            uint headerCount = 0;
            if (request.Headers != null) {
                headerCount = (uint) request.Headers.Length;
            }

            HttpRequestSigningCallback callback = new HttpRequestSigningCallback();
            callback.OriginalRequest = request; /* needed to build final signed request */
            callback.ShouldSignHeader = signingConfig.ShouldSignHeader; /* prevent GC while signing */
            callback.BodyStream = new CrtStreamWrapper(request.BodyStream);

            ulong id = PendingHttpRequestSignings.AcquireStrongReference(callback);

            API.SignRequestNative(request.Method, request.Uri, request.Headers, headerCount, callback.BodyStream.Delegates, nativeConfig, id, API.OnHttpRequestSigningComplete);

            return callback.TaskSource.Task;
        }

        private static void OnCanonicalRequestSigningComplete(ulong id, int errorCode, string authorizationValue)
        {
            CanonicalRequestSigningCallback callback = PendingCanonicalRequestSignings.ReleaseStrongReference(id);
            if (callback == null) {
                return;
            }

            if (errorCode != 0)
            {
                callback.TaskSource.SetException(new CrtException(errorCode));
            }
            else
            {
                callback.TaskSource.SetResult(authorizationValue);
            }
        }

        public static Task<String> SignCanonicalRequest(String canonicalRequest, AwsSigningConfig signingConfig) 
        {
            if (canonicalRequest == null || signingConfig == null) {
                throw new CrtException("Null argument passed to SignRequest");
            }

            if (signingConfig.SignatureType != AwsSignatureType.CANONICAL_REQUEST_VIA_HEADERS && 
                signingConfig.SignatureType != AwsSignatureType.CANONICAL_REQUEST_VIA_QUERY_PARAMS) {
                throw new CrtException("Illegal signing type for canonical request signing");
            }

            var nativeConfig = new AwsSigningConfigNative(signingConfig);

            CanonicalRequestSigningCallback callback = new CanonicalRequestSigningCallback();
            callback.OriginalCanonicalRequest = canonicalRequest;

            ulong id = PendingCanonicalRequestSignings.AcquireStrongReference(callback);

            API.SignCanonicalRequestNative(canonicalRequest, nativeConfig, id, API.OnCanonicalRequestSigningComplete);

            return callback.TaskSource.Task;
        }        
    }
}
