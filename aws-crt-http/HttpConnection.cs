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
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt.IO;

namespace Aws.Crt.Http
{
    public delegate void OnConnectionSetup(int errorCode);
    public delegate void OnConnectionShutdown(int errorCode);
    public delegate void OnStreamOutgoingBody(HttpStream stream, out byte[] buffer);
    public delegate void OnIncomingHeaders(HttpStream stream, HttpHeader[] headers);
    public delegate void OnIncomingHeaderBlockDone(HttpStream stream, bool hasBody);
    public delegate void OnIncomingBody(HttpStream stream, byte[] data);
    public delegate void OnStreamComplete(HttpStream stream, int errorCode);

    internal delegate void OnStreamOutgoingBodyNative(
        [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] out byte[] buffer, 
        [Out] out UInt64 size);
    internal delegate void OnIncomingHeadersNative(
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] HttpHeader[] headers, 
        UInt32 count);
    internal delegate void OnIncomingHeaderBlockDoneNative(bool hasBody);
    internal delegate void OnIncomingBodyNative(
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] buffer, 
        UInt64 size);
    internal delegate void OnStreamCompleteNative(int errorCode);

    public sealed class HttpRequestOptions
    {
        public string Method { get; set; }
        public Uri Uri { get; set; }
        public HttpHeader[] Headers { get; set; }
        public OnStreamOutgoingBody OnStreamOutgoingBody { get; set; }
        public OnStreamComplete OnStreamComplete { get; set; }
        public OnIncomingHeaders OnIncomingHeaders { get; set; }
        public OnIncomingHeaderBlockDone OnIncomingHeaderBlockDone { get; set; }
        public OnIncomingBody OnIncomingBody { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
    public struct HttpHeader
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string Name;

        [MarshalAs(UnmanagedType.LPStr)]
        public string Value;

        public HttpHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public abstract class HttpStream
    {
        [SecuritySafeCritical]
        internal static class API
        {
            static LibraryHandle library = new LibraryHandle();
            public delegate Handle aws_dotnet_http_stream_new(
                                    IntPtr connection,
                                    [MarshalAs(UnmanagedType.LPStr)] string method,
                                    [MarshalAs(UnmanagedType.LPStr)] string uri,
                                    [In] HttpHeader[] headers,
                                    UInt32 header_count,
                                    OnStreamOutgoingBodyNative onStreamOutgoingBody,
                                    OnIncomingHeadersNative onIncomingHeaders,
                                    OnIncomingHeaderBlockDoneNative onIncomingHeaderBlockDone,
                                    OnIncomingBodyNative onIncomingBody,
                                    OnStreamCompleteNative onStreamComplete);
            public delegate void aws_dotnet_http_stream_destroy(IntPtr stream);
            public delegate void aws_dotnet_http_stream_update_window(IntPtr stream, UInt64 incrementSize);

            public static aws_dotnet_http_stream_new make_new = NativeAPI.Bind<aws_dotnet_http_stream_new>();
            public static aws_dotnet_http_stream_destroy destroy = NativeAPI.Bind<aws_dotnet_http_stream_destroy>();
            public static aws_dotnet_http_stream_update_window update_window = NativeAPI.Bind<aws_dotnet_http_stream_update_window>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        internal Handle NativeHandle { get; private set; }

        public HttpClientConnection Connection { get; protected set; }
        public int ResponseStatusCode { get; protected set; }

        // reference to options used to create this stream, which keeps the callbacks alive
        // for the duration of the stream
        private HttpRequestOptions options;

        public HttpStream(HttpClientConnection connection, HttpRequestOptions options) 
        {
            if (options.OnIncomingHeaders == null)
                throw new ArgumentNullException("OnIncomingHeaders");
            if (options.OnStreamComplete == null)
                throw new ArgumentNullException("OnStreamComplete");

            this.Connection = connection;

            // Wrap the native callbacks to bind this stream to them as the first argument
            OnStreamOutgoingBodyNative onStreamOutgoingBody = (out byte[] buffer, out UInt64 size) =>
            {
                options.OnStreamOutgoingBody(this, out buffer);
                size = (UInt32)buffer.Length;
            };
            OnIncomingHeadersNative onIncomingHeaders = (headers, headerCount) =>
            {
                options.OnIncomingHeaders(this, headers);
            };
            OnIncomingHeaderBlockDoneNative onIncomingHeaderBlockDone = (hasBody) =>
            {
                options.OnIncomingHeaderBlockDone(this, hasBody);
            };
            OnIncomingBodyNative onIncomingBody = (data, size) =>
            {
                options.OnIncomingBody(this, data);
            };
            OnStreamCompleteNative onStreamComplete = (errorCode) =>
            {
                options.OnStreamComplete(this, errorCode);
            };
            NativeHandle = API.make_new(
                connection.NativeHandle.DangerousGetHandle(),
                options.Method,
                options.Uri.PathAndQuery,
                options.Headers,
                (UInt32)(options.Headers?.Length ?? 0),
                options.OnStreamOutgoingBody != null ? onStreamOutgoingBody : null,
                onIncomingHeaders,
                options.OnIncomingHeaderBlockDone != null ? onIncomingHeaderBlockDone : null,
                options.OnIncomingBody != null ? onIncomingBody : null,
                onStreamComplete);
            this.options = options;
        }

        public void UpdateWindow(uint incrementSize)
        {
            API.update_window(NativeHandle.DangerousGetHandle(), incrementSize);
        }
    }

    public sealed class HttpClientStream : HttpStream
    {
        public HttpClientStream(HttpClientConnection connection, HttpRequestOptions options)
            : base(connection, options)
        {            
        }
    }

    public sealed class HttpClientConnectionOptions
    {
        public ClientBootstrap ClientBootstrap { get; set; }
        public uint InitialWindowSize { get; set; }
        public string HostName { get; set; }
        public UInt16 Port { get; set; }
        public SocketOptions SocketOptions { get; set; }
        public TlsConnectionOptions TlsConnectionOptions { get; set; }
        public OnConnectionSetup OnConnectionSetup { get; set; }
        public OnConnectionShutdown OnConnectionShutdown { get; set; }
    }

    public sealed class HttpClientConnection
    {
        [SecuritySafeCritical]
        internal static class API
        {
            static LibraryHandle library = new LibraryHandle();
            public delegate Handle aws_dotnet_http_connection_new(
                                    IntPtr clientBootstrap,
                                    UInt64 initialWindowSize,
                                    [MarshalAs(UnmanagedType.LPStr)] string hostName,
                                    UInt16 port,
                                    IntPtr socketOptions,
                                    IntPtr tlsConnectionOptions,
                                    OnConnectionSetup onSetup,
                                    OnConnectionShutdown onShutdown);
            public delegate void aws_dotnet_http_connection_destroy(IntPtr connection);

            public static aws_dotnet_http_connection_new make_new = NativeAPI.Bind<aws_dotnet_http_connection_new>();
            public static aws_dotnet_http_connection_destroy destroy = NativeAPI.Bind<aws_dotnet_http_connection_destroy>();
        }

        internal class Handle : CRT.Handle
        {
            protected override bool ReleaseHandle()
            {
                API.destroy(handle);
                return true;
            }
        }

        internal Handle NativeHandle { get; private set; }
        private HttpClientConnectionOptions Options;

        public HttpClientConnection(HttpClientConnectionOptions options)
        {
            if (options.ClientBootstrap == null)
                throw new ArgumentNullException("ClientBootstrap");
            if (options.OnConnectionSetup == null)
                throw new ArgumentNullException("OnConnectionSetup");
            if (options.OnConnectionShutdown == null)
                throw new ArgumentNullException("OnConnectionShutdown");
            if (options.HostName == null)
                throw new ArgumentNullException("HostName");
            if (options.Port == 0)
                throw new ArgumentOutOfRangeException("Port", options.Port, "Port must be between 1 and 65535");

            this.Options = options;
            NativeHandle = API.make_new(
                options.ClientBootstrap.NativeHandle.DangerousGetHandle(),
                options.InitialWindowSize,
                options.HostName,
                options.Port,
                options.SocketOptions?.NativeHandle.DangerousGetHandle() ?? IntPtr.Zero,
                options.TlsConnectionOptions?.NativeHandle.DangerousGetHandle() ?? IntPtr.Zero,
                options.OnConnectionSetup,
                options.OnConnectionShutdown);
        }
    }
}
