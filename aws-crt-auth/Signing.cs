/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

using Aws.Crt;
using Aws.Crt.Http;

namespace Aws.Crt.Auth
{
    public enum AwsSigningAlgorithm {
        SIGV4 = 0
    }

    public enum AwsSignatureType {
        HTTP_REQUEST_VIA_HEADERS = 0,
        HTTP_REQUEST_VIA_QUERY_PARAMS = 1,
        HTTP_REQUEST_CHUNK = 2,
        HTTP_REQUEST_EVENT = 3
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

    public delegate bool aws_dotnet_auth_should_sign_header([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)]byte[] headerName);

    public class AwsSigningConfig {

        public AwsSigningAlgorithm Algorithm { get; set; }
        public AwsSignatureType SignatureType { get; set; }
        public string Region { get; set; }
        public string Service { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Credentials Credentials { get; set; }
        public aws_dotnet_auth_should_sign_header ShouldSignHeader;
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

            public aws_dotnet_auth_should_sign_header ShouldSignHeader;

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
            internal delegate void OnSigningComplete(
                UInt64 id, 
                Int32 error_code, 
                [MarshalAs(UnmanagedType.LPStr)] string uri, 
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)] HttpHeader[] headers, 
                UInt32 count);

            internal delegate void aws_dotnet_auth_sign_request(
                                    [MarshalAs(UnmanagedType.LPStr)] string method,
                                    [MarshalAs(UnmanagedType.LPStr)] string uri,
                                    [In] HttpHeader[] headers,
                                    UInt32 header_count,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    OnSigningComplete completion_callback_delegate);

            public static aws_dotnet_auth_sign_request sign_request = NativeAPI.Bind<aws_dotnet_auth_sign_request>();

            public static OnSigningComplete on_signing_complete = AwsSigner.OnSigningComplete;

            private static LibraryHandle library = new LibraryHandle();
        }

        private class SigningCallback
        {
            public HttpRequest OriginalRequest;
            public TaskCompletionSource<HttpRequest> TaskSource = new TaskCompletionSource<HttpRequest>();
        }

        private static StrongReferenceVendor<SigningCallback> pendingCallbacks = new StrongReferenceVendor<SigningCallback>();

        private static void OnSigningComplete(ulong id, int errorCode, string uri, HttpHeader[] headers, uint headerCount)
        {
            SigningCallback continuation = pendingCallbacks.UnwrapStrongReference(id);
            if (continuation == null) {
                return;
            }

            if (errorCode != 0)
            {
                continuation.TaskSource.SetException(new CrtException(errorCode));
            }
            else
            {
                HttpRequest sourceRequest = continuation.OriginalRequest;
                HttpRequest signedRequest = new HttpRequest();
                signedRequest.Method = sourceRequest.Method;
                signedRequest.Uri = uri;
                signedRequest.Headers = headers;
                signedRequest.BodyStream = sourceRequest.BodyStream;

                continuation.TaskSource.SetResult(signedRequest);
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

                request.BodyStream.Seek(0, SeekOrigin.Begin);
            }

            var nativeConfig = new AwsSigningConfigNative(signingConfig);

            uint headerCount = 0;
            if (request.Headers != null) {
                headerCount = (uint) request.Headers.Length;
            }

            SigningCallback callback = new SigningCallback();
            callback.OriginalRequest = request;

            ulong id = pendingCallbacks.WrapStrongReference(callback);

            API.sign_request(request.Method, request.Uri, request.Headers, headerCount, nativeConfig, id, API.on_signing_complete);

            return callback.TaskSource.Task;
        }
    }
}