/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;

using Aws.Crt.Http;
using Aws.Crt.IO;

namespace Aws.Crt.Auth
{
    public enum AwsSigningAlgorithm {
        SIGV4 = 0,
        SIGV4A = 1,
    }

    public enum AwsSignatureType {
        HTTP_REQUEST_VIA_HEADERS = 0,
        HTTP_REQUEST_VIA_QUERY_PARAMS = 1,
        HTTP_REQUEST_CHUNK = 2,
        HTTP_REQUEST_EVENT = 3,
        CANONICAL_REQUEST_VIA_HEADERS = 4,
        CANONICAL_REQUEST_VIA_QUERY_PARAMS = 5,
        HTTP_REQUEST_TRAILING_HEADERS = 6,
    }

    public class AwsSignedBodyValue {
        public static string EMPTY_SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        public static string UNSIGNED_PAYLOAD = "UNSIGNED-PAYLOAD";
        public static string STREAMING_AWS4_HMAC_SHA256_PAYLOAD = "STREAMING-AWS4-HMAC-SHA256-PAYLOAD";
        public static string STREAMING_AWS4_ECDSA_P256_SHA256_PAYLOAD = "STREAMING-AWS4-ECDSA-P256-SHA256-PAYLOAD";
        public static string STREAMING_AWS4_ECDSA_P256_SHA256_PAYLOAD_TRAILER = "STREAMING-AWS4-ECDSA-P256-SHA256-PAYLOAD-TRAILER";
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
                MillisecondsSinceEpoch = MillisecondsSinceEpoch = (long)(config.Timestamp.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

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

        public class CrtSigningResult {
            public byte[] Signature;

            public HttpRequest SignedRequest;
        }


        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        private delegate int aws_dotnet_get_native_memory_usage();

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        private delegate void aws_dotnet_native_memory_dump();

        internal static class API
        {
            internal delegate void OnSigningCompleteCallback(
                UInt64 id,
                Int32 errorCode,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=3)] byte[] signatureBuffer,
                UInt64 signatureBufferSize,
                [MarshalAs(UnmanagedType.LPStr)] string signedUri,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=6)] HttpHeaderNative[] signedHeaders,
                UInt32 signedHeaderCount);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate void AwsDotnetAuthSignHttpRequest(
                                    [MarshalAs(UnmanagedType.LPStr)] string method,
                                    [MarshalAs(UnmanagedType.LPStr)] string uri,
                                    [In] HttpHeader[] headers,
                                    UInt32 header_count,
                                    [In] CrtStreamWrapper.DelegateTable stream_delegate_table,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    OnSigningCompleteCallback completion_callback_delegate);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate void AwsDotnetAuthSignCanonicalRequest(
                                    [MarshalAs(UnmanagedType.LPStr)] string canonical_request,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    OnSigningCompleteCallback completion_callback_delegate);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate void AwsDotnetAuthSignChunk(
                                    [In] CrtStreamWrapper.DelegateTable stream_delegate_table,
                                    [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2, ArraySubType=UnmanagedType.U1)] byte[] signature_buffer,
                                    UInt32 signature_buffer_length,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    OnSigningCompleteCallback completion_callback_delegate);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate int GetNativeMem();
            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate void MemDump();

            internal delegate void AwsDotnetAuthSignTrailingHeaders(
                                    [In] HttpHeader[] headers,
                                    UInt32 header_count,
                                    [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2, ArraySubType=UnmanagedType.U1)] byte[] signature_buffer,
                                    UInt32 signature_buffer_length,
                                    [In] AwsSigningConfigNative signing_config,
                                    UInt64 future_id,
                                    OnSigningCompleteCallback completion_callback_delegate);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate bool AwsDotnetAuthVerifyV4aCanonicalSigning(
                                    [MarshalAs(UnmanagedType.LPStr)] string canonical_request,
                                    [In] AwsSigningConfigNative signing_config,
                                    [MarshalAs(UnmanagedType.LPStr)] string signature,
                                    [MarshalAs(UnmanagedType.LPStr)] string ecc_pub_x,
                                    [MarshalAs(UnmanagedType.LPStr)] string ecc_pub_y);

            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            internal delegate bool AwsDotnetAuthVerifyV4aSignature(
                                    [MarshalAs(UnmanagedType.LPStr)] string string_to_sign,
                                    [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2, ArraySubType=UnmanagedType.U1)] byte[] signature_buffer,
                                    UInt32 signature_buffer_length,
                                    [MarshalAs(UnmanagedType.LPStr)] string ecc_pub_x,
                                    [MarshalAs(UnmanagedType.LPStr)] string ecc_pub_y);

            public static AwsDotnetAuthSignHttpRequest SignRequestNative = NativeAPI.Bind<AwsDotnetAuthSignHttpRequest>("aws_dotnet_auth_sign_http_request");

            public static AwsDotnetAuthSignCanonicalRequest SignCanonicalRequestNative = NativeAPI.Bind<AwsDotnetAuthSignCanonicalRequest>("aws_dotnet_auth_sign_canonical_request");

            public static AwsDotnetAuthSignChunk SignChunkNative = NativeAPI.Bind<AwsDotnetAuthSignChunk>("aws_dotnet_auth_sign_chunk");

            public static GetNativeMem GetMemNative = NativeAPI.Bind<GetNativeMem>("aws_dotnet_get_native_memory_usage");
            public static MemDump MemDumpNative = NativeAPI.Bind<MemDump>("aws_dotnet_native_memory_dump");

            public static AwsDotnetAuthSignTrailingHeaders SignTrailingHeadersNative = NativeAPI.Bind<AwsDotnetAuthSignTrailingHeaders>("aws_dotnet_auth_sign_trailing_headers");

            public static AwsDotnetAuthVerifyV4aCanonicalSigning VerifyV4aCanonicalSigningNative = NativeAPI.Bind<AwsDotnetAuthVerifyV4aCanonicalSigning>("aws_dotnet_auth_verify_v4a_canonical_signing");

            public static AwsDotnetAuthVerifyV4aSignature VerifyV4aSignatureNative = NativeAPI.Bind<AwsDotnetAuthVerifyV4aSignature>("aws_dotnet_auth_verify_v4a_signature");

            public static OnSigningCompleteCallback OnHttpRequestSigningComplete = AwsSigner.OnHttpRequestSigningComplete;

            public static OnSigningCompleteCallback OnCanonicalRequestSigningComplete = AwsSigner.OnCanonicalRequestSigningComplete;

            public static OnSigningCompleteCallback OnChunkSigningComplete = AwsSigner.OnChunkSigningComplete;

            public static OnSigningCompleteCallback OnTrailingHeadersSigningComplete = AwsSigner.OnTrailingHeadersSigningComplete;

            private static LibraryHandle library = new LibraryHandle();



        }

        private class HttpRequestSigningCallbackData
        {
            public HttpRequest OriginalRequest;
            public CrtResult<CrtSigningResult> Result = new CrtResult<CrtSigningResult>();
            public CrtStreamWrapper BodyStream;
            public ShouldSignHeaderCallback ShouldSignHeader;
        }

        private class CanonicalRequestSigningCallbackData
        {
            public String OriginalCanonicalRequest;
            public CrtResult<CrtSigningResult> Result = new CrtResult<CrtSigningResult>();
        }

        private class ChunkSigningCallbackData
        {
            public CrtStreamWrapper OriginalChunkBodyStream;

            public byte[] PreviousSignature;

            public CrtResult<CrtSigningResult> Result = new CrtResult<CrtSigningResult>();
        }

        private class TrailingHeadersSigningCallbackData
        {

            public byte[] PreviousSignature;

            public HttpHeader[] TrailingHeaders;

            public CrtResult<CrtSigningResult> Result = new CrtResult<CrtSigningResult>();
        }

        private static StrongReferenceVendor<HttpRequestSigningCallbackData> PendingHttpRequestSignings = new StrongReferenceVendor<HttpRequestSigningCallbackData>();
        private static StrongReferenceVendor<CanonicalRequestSigningCallbackData> PendingCanonicalRequestSignings = new StrongReferenceVendor<CanonicalRequestSigningCallbackData>();
        private static StrongReferenceVendor<ChunkSigningCallbackData> PendingChunkSignings = new StrongReferenceVendor<ChunkSigningCallbackData>();
        private static StrongReferenceVendor<TrailingHeadersSigningCallbackData> PendingTrailingHeadersSignings = new StrongReferenceVendor<TrailingHeadersSigningCallbackData>();

        private static void OnHttpRequestSigningComplete(ulong id, int errorCode, byte[] signatureBuffer, ulong signatureBufferSize, string uri, HttpHeaderNative[] headers, uint headerCount)
        {
            HttpRequestSigningCallbackData callback = PendingHttpRequestSignings.ReleaseStrongReference(id);
            if (callback == null) {
                return;
            }

            if (errorCode != 0)
            {
                callback.Result.CompleteExceptionally(new CrtException(errorCode));
            }
            else
            {
                HttpRequest sourceRequest = callback.OriginalRequest;
                HttpRequest signedRequest = new HttpRequest();
                signedRequest.Method = sourceRequest.Method;
                signedRequest.Uri = uri;
                signedRequest.Headers = Array.ConvertAll(headers, header => new HttpHeader(header.Name, header.Value));
                signedRequest.BodyStream = sourceRequest.BodyStream;

                CrtSigningResult result = new CrtSigningResult();
                result.SignedRequest = signedRequest;
                result.Signature = signatureBuffer;

                callback.Result.Complete(result);
            }
        }

        public static bool CheckForLeak()
        {
            GC.Collect();
            int leak = API.GetMemNative();

            if(leak>0) {
                Console.WriteLine($"Leak detected: {leak} bytes");

                API.MemDumpNative();
                return true;
            }
            return false;
        }
        public static int GetMem()
        {
            GC.Collect();
            return API.GetMemNative();
        }

        public static void MemDump()
        {
            API.MemDumpNative();
        }

        public static CrtResult<CrtSigningResult> SignHttpRequest(HttpRequest request, AwsSigningConfig signingConfig)
        {
            if (request == null || signingConfig == null) {
                throw new CrtException("Null argument passed to SignHttpRequest");
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

            HttpRequestSigningCallbackData callback = new HttpRequestSigningCallbackData();
            callback.OriginalRequest = request; /* needed to build final signed request */
            callback.ShouldSignHeader = signingConfig.ShouldSignHeader; /* prevent GC while signing */
            callback.BodyStream = new CrtStreamWrapper(request.BodyStream);

            ulong id = PendingHttpRequestSignings.AcquireStrongReference(callback);

            API.SignRequestNative(request.Method, request.Uri, request.Headers, headerCount, callback.BodyStream.Delegates, nativeConfig, id, API.OnHttpRequestSigningComplete);

            return callback.Result;
        }

        private static void OnCanonicalRequestSigningComplete(ulong id, int errorCode, byte[] signatureBuffer, ulong signatureBufferSize, string uri, HttpHeaderNative[] headers, uint headerCount)
        {
            CanonicalRequestSigningCallbackData callback = PendingCanonicalRequestSignings.ReleaseStrongReference(id);
            if (callback == null) {
                return;
            }

            if (errorCode != 0)
            {
                callback.Result.CompleteExceptionally(new CrtException(errorCode));
            }
            else
            {
                CrtSigningResult result = new CrtSigningResult();
                result.Signature = signatureBuffer;

                callback.Result.Complete(result);
            }
        }

        public static bool VerifyV4aCanonicalSigning(String canonicalRequest, AwsSigningConfig signingConfig, String hexSignature, String eccPubX, String eccPubY) {
            var nativeConfig = new AwsSigningConfigNative(signingConfig);
            return API.VerifyV4aCanonicalSigningNative(canonicalRequest, nativeConfig, hexSignature, eccPubX, eccPubY);
        }

        public static bool VerifyV4aSignature(String stringToSign, byte[] signature, String eccPubX, String eccPubY) {
            return API.VerifyV4aSignatureNative(stringToSign, signature, (uint) signature.Length, eccPubX, eccPubY);
        }

        public static CrtResult<CrtSigningResult> SignCanonicalRequest(String canonicalRequest, AwsSigningConfig signingConfig)
        {
            if (canonicalRequest == null || signingConfig == null) {
                throw new CrtException("Null argument passed to SignCanonicalRequest");
            }

            if (signingConfig.SignatureType != AwsSignatureType.CANONICAL_REQUEST_VIA_HEADERS &&
                signingConfig.SignatureType != AwsSignatureType.CANONICAL_REQUEST_VIA_QUERY_PARAMS) {
                throw new CrtException("Illegal signing type for canonical request signing");
            }

            var nativeConfig = new AwsSigningConfigNative(signingConfig);

            CanonicalRequestSigningCallbackData callback = new CanonicalRequestSigningCallbackData();
            callback.OriginalCanonicalRequest = canonicalRequest;

            ulong id = PendingCanonicalRequestSignings.AcquireStrongReference(callback);

            API.SignCanonicalRequestNative(canonicalRequest, nativeConfig, id, API.OnCanonicalRequestSigningComplete);

            return callback.Result;
        }

        private static void OnChunkSigningComplete(ulong id, int errorCode, byte[] signatureBuffer, ulong signatureBufferSize, string uri, HttpHeaderNative[] headers, uint headerCount)
        {
            ChunkSigningCallbackData callback = PendingChunkSignings.ReleaseStrongReference(id);
            if (callback == null) {
                return;
            }

            if (errorCode != 0)
            {
                callback.Result.CompleteExceptionally(new CrtException(errorCode));
            }
            else
            {
                CrtSigningResult result = new CrtSigningResult();
                result.Signature = signatureBuffer;

                callback.Result.Complete(result);
            }
        }

        public static CrtResult<CrtSigningResult> SignChunk(Stream chunkBodyStream, byte[] previousSignature, AwsSigningConfig signingConfig)
        {
            if (previousSignature == null || signingConfig == null) {
                throw new CrtException("Null argument passed to SignChunk");
            }

            if (signingConfig.SignatureType != AwsSignatureType.HTTP_REQUEST_CHUNK) {
                throw new CrtException("Illegal signature type for chunked body signing");
            }

            var nativeConfig = new AwsSigningConfigNative(signingConfig);

            ChunkSigningCallbackData callback = new ChunkSigningCallbackData();
            callback.OriginalChunkBodyStream = new CrtStreamWrapper(chunkBodyStream);
            callback.PreviousSignature = previousSignature;

            ulong id = PendingChunkSignings.AcquireStrongReference(callback);

            API.SignChunkNative(callback.OriginalChunkBodyStream.Delegates, callback.PreviousSignature, (uint) callback.PreviousSignature.Length, nativeConfig, id, API.OnChunkSigningComplete);

            return callback.Result;
        }

        private static void OnTrailingHeadersSigningComplete(ulong id, int errorCode, byte[] signatureBuffer, ulong signatureBufferSize, string uri, HttpHeaderNative[] headers, uint headerCount)
        {
            TrailingHeadersSigningCallbackData callback = PendingTrailingHeadersSignings.ReleaseStrongReference(id);
            if (callback == null) {
                return;
            }

            if (errorCode != 0)
            {
                callback.Result.CompleteExceptionally(new CrtException(errorCode));
            }
            else
            {
                CrtSigningResult result = new CrtSigningResult();
                result.Signature = signatureBuffer;

                callback.Result.Complete(result);
            }
        }

        public static CrtResult<CrtSigningResult> SignTrailingHeaders(HttpHeader[] headers, byte[] previousSignature, AwsSigningConfig signingConfig)
        {
            if (previousSignature == null || signingConfig == null) {
                throw new CrtException("Null argument passed to SignChunk");
            }

            if (signingConfig.SignatureType != AwsSignatureType.HTTP_REQUEST_TRAILING_HEADERS) {
                throw new CrtException("Illegal signature type for trailing headers signing");
            }

            uint trailingHeaderCount = 0;
            if (headers != null) {
                trailingHeaderCount = (uint) headers.Length;
            }

            var nativeConfig = new AwsSigningConfigNative(signingConfig);

            TrailingHeadersSigningCallbackData callback = new TrailingHeadersSigningCallbackData();
            callback.TrailingHeaders = headers;
            callback.PreviousSignature = previousSignature;

            ulong id = PendingTrailingHeadersSignings.AcquireStrongReference(callback);

            API.SignTrailingHeadersNative(callback.TrailingHeaders, trailingHeaderCount, callback.PreviousSignature, (uint) callback.PreviousSignature.Length, nativeConfig, id, API.OnTrailingHeadersSigningComplete);

            return callback.Result;
        }
    }
}
