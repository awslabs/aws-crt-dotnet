/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;

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
            internal delegate void aws_dotnet_auth_sign_request([In, MarshalAs(UnmanagedType.Struct)] HttpRequestNative request, [In, MarshalAs(UnmanagedType.Struct)] AwsSigningConfigNative signingConfig);

            public static aws_dotnet_auth_sign_request sign_request = NativeAPI.Bind<aws_dotnet_auth_sign_request>();

            private static LibraryHandle library = new LibraryHandle();
        }

        public static void SignRequest(HttpRequest request, AwsSigningConfig signingConfig) {
            var nativeRequest = new HttpRequestNative(request);
            var nativeConfig = new AwsSigningConfigNative(signingConfig);
            
            API.sign_request(nativeRequest, nativeConfig);
        }
    }
}