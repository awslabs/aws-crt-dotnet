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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt.IO;

namespace Aws.Crt.Http
{
    public enum OutgoingBodyStreamState 
    {
        InProgress = 0,
        Done = 1,
    }

    public abstract class HttpClientStreamEventArgs : EventArgs
    {
        public HttpClientStream Stream { get; private set; }

        public HttpClientStreamEventArgs(HttpClientStream stream)
        {
            Stream = stream;
        }
    }

    public class IncomingHeadersEventArgs : HttpClientStreamEventArgs
    {
        public HttpHeader[] Headers { get; private set; }

        internal IncomingHeadersEventArgs(HttpClientStream stream, HttpHeader[] headers)
            : base(stream)
        {
            Headers = headers;
        }
    }

    public class IncomingHeadersDoneEventArgs : HttpClientStreamEventArgs
    {
        public bool HasBody { get; private set; }

        internal IncomingHeadersDoneEventArgs(HttpClientStream stream, bool hasBody)
            : base(stream)
        {
            HasBody = hasBody;
        }
    }

    public class IncomingBodyEventArgs : HttpClientStreamEventArgs
    {
        public byte[] Data { get; private set; }
        public UInt64 WindowSize { get; private set; }

        internal IncomingBodyEventArgs(HttpClientStream stream, byte[] data, UInt64 windowSize)
            : base(stream)
        {
            Data = data;
            WindowSize = windowSize;
        }
    }

    public class StreamOutgoingBodyEventArgs : HttpClientStreamEventArgs
    {
        public byte[] Buffer { get; private set; }
        public OutgoingBodyStreamState State { get; set; } = OutgoingBodyStreamState.InProgress;
        public UInt64 BytesWritten { get; set; } = UInt64.MaxValue;

        internal StreamOutgoingBodyEventArgs(HttpClientStream stream, byte[] buffer)
            : base(stream)
        {
            Buffer = buffer;
        }
    }

    public class StreamCompleteEventArgs : HttpClientStreamEventArgs
    {
        public int ErrorCode { get; private set; }

        internal StreamCompleteEventArgs(HttpClientStream stream, int errorCode)
            : base(stream)
        {
            ErrorCode = errorCode;
        }
    }

    public sealed class HttpRequestOptions
    {
        public string Method { get; set; }
        public string Uri { get; set; }
        public HttpHeader[] Headers { get; set; }
        public event EventHandler<StreamOutgoingBodyEventArgs> StreamOutgoingBody;
        public event EventHandler<StreamCompleteEventArgs> StreamComplete;
        public event EventHandler<IncomingHeadersEventArgs> IncomingHeaders;
        public event EventHandler<IncomingHeadersDoneEventArgs> IncomingHeadersDone;
        public event EventHandler<IncomingBodyEventArgs> IncomingBody;

        internal void Validate()
        {
            if (IncomingHeaders == null)
                throw new ArgumentNullException("IncomingHeaders");
            if (StreamComplete == null)
                throw new ArgumentNullException("StreamComplete");
        }

        internal OutgoingBodyStreamState OnStreamOutgoingBody(HttpClientStream stream, byte[] buffer, out UInt64 bytesWritten)
        {
            var e = new StreamOutgoingBodyEventArgs(stream, buffer);
            StreamOutgoingBody?.Invoke(stream, e);
            if (e.BytesWritten > (UInt64)buffer.Length)
                throw new ArgumentOutOfRangeException("BytesWritten should be set to the number of bytes written to the buffer");
            bytesWritten = e.BytesWritten;
            return e.State;
        }

        internal void OnStreamComplete(HttpClientStream stream, int errorCode)
        {
            StreamComplete?.Invoke(stream, new StreamCompleteEventArgs(stream, errorCode));
        }

        internal void OnIncomingHeaders(HttpClientStream stream, HttpHeader[] headers)
        {
            IncomingHeaders?.Invoke(stream, new IncomingHeadersEventArgs(stream, headers));
        }

        internal void OnIncomingHeadersDone(HttpClientStream stream, bool hasBody)
        {
            IncomingHeadersDone?.Invoke(stream, new IncomingHeadersDoneEventArgs(stream, hasBody));
        }

        internal void OnIncomingBody(HttpClientStream stream, byte[] data, ref UInt64 windowSize)
        {
            var e = new IncomingBodyEventArgs(stream, data, windowSize);
            IncomingBody?.Invoke(stream, e);
            windowSize = e.WindowSize;
        }
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
            internal delegate int OnStreamOutgoingBodyNative(
                                    [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] buffer, 
                                    UInt64 size,
                                    out UInt64 bytesWritten);
            internal delegate void OnIncomingHeadersNative(
                                    Int32 responseCode,
                                    [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)] HttpHeader[] headers, 
                                    UInt32 count);
            internal delegate void OnIncomingHeaderBlockDoneNative(bool hasBody);
            internal delegate void OnIncomingBodyNative(
                                    [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] buffer, 
                                    UInt64 size,
                                    ref UInt64 windowSize);
            internal delegate void OnStreamCompleteNative(int errorCode);

            private static LibraryHandle library = new LibraryHandle();
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

        internal Handle NativeHandle { get; set; }

        public HttpClientConnection Connection { get; protected set; }

        public HttpStream(HttpClientConnection connection) 
        {
            this.Connection = connection;
        }

        public void UpdateWindow(uint incrementSize)
        {
            API.update_window(NativeHandle.DangerousGetHandle(), incrementSize);
        }
    }

    public sealed class HttpClientStream : HttpStream
    {
        public int ResponseStatusCode { get; private set; }

        // Reference to options used to create this stream, which keeps the callbacks alive
        // for the duration of the stream
        private HttpRequestOptions options;
        // References to native callbacks to keep them alive for the duration of the stream
        private List<Delegate> nativeCallbacks = new List<Delegate>();

        internal HttpClientStream(HttpClientConnection connection, HttpRequestOptions options)
            : base(connection)
        {            
            options.Validate();
            
            // Wrap the native callbacks to bind this stream to them as the first argument
            API.OnStreamOutgoingBodyNative onStreamOutgoingBody = (byte[] buffer, UInt64 size, out UInt64 bytesWritten) =>
            {
                return (int)options.OnStreamOutgoingBody(this, buffer, out bytesWritten);
            };
            API.OnIncomingHeadersNative onIncomingHeaders = (responseCode, headers, headerCount) =>
            {
                if (ResponseStatusCode == 0) {
                    ResponseStatusCode = responseCode;
                }
                options.OnIncomingHeaders(this, headers);
            };
            API.OnIncomingHeaderBlockDoneNative onIncomingHeaderBlockDone = (hasBody) =>
            {
                options.OnIncomingHeadersDone(this, hasBody);
            };
            API.OnIncomingBodyNative onIncomingBody = (byte[] data, UInt64 size, ref UInt64 windowSize) =>
            {
                options.OnIncomingBody(this, data, ref windowSize);
            };
            API.OnStreamCompleteNative onStreamComplete = (errorCode) =>
            {
                options.OnStreamComplete(this, errorCode);
            };
            this.options = options;
            nativeCallbacks.AddRange(new Delegate[] {
                onStreamOutgoingBody,
                onIncomingHeaders,
                onIncomingHeaderBlockDone,
                onIncomingBody,
                onStreamComplete
            });
            NativeHandle = API.make_new(
                connection.NativeHandle.DangerousGetHandle(),
                options.Method,
                options.Uri,
                options.Headers,
                (UInt32)(options.Headers?.Length ?? 0),
                onStreamOutgoingBody,
                onIncomingHeaders,
                onIncomingHeaderBlockDone,
                onIncomingBody,
                onStreamComplete);
        }
    }

    public sealed class ConnectionSetupEventArgs : EventArgs
    {
        public int ErrorCode { get; private set; }

        internal ConnectionSetupEventArgs(int errorCode)
        {
            ErrorCode = errorCode;
        }
    }

    public sealed class ConnectionShutdownEventArgs : EventArgs
    {
        public int ErrorCode { get; private set; }

        internal ConnectionShutdownEventArgs(int errorCode)
        {
            ErrorCode = errorCode;
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
        public event EventHandler<ConnectionSetupEventArgs> ConnectionSetup;
        public event EventHandler<ConnectionShutdownEventArgs> ConnectionShutdown;

        internal void Validate()
        {
            if (ClientBootstrap == null)
                throw new ArgumentNullException("ClientBootstrap");
            if (ConnectionSetup == null)
                throw new ArgumentNullException("ConnectionSetup");
            if (ConnectionShutdown == null)
                throw new ArgumentNullException("ConnectionShutdown");
            if (HostName == null)
                throw new ArgumentNullException("HostName");
            if (Port == 0)
                throw new ArgumentOutOfRangeException("Port", Port, "Port must be between 1 and 65535");
        }

        internal void OnConnectionSetup(HttpClientConnection sender, int errorCode)
        {
            ConnectionSetup?.Invoke(sender, new ConnectionSetupEventArgs(errorCode));
        }

        internal void OnConnectionShutdown(HttpClientConnection sender, int errorCode)
        {
            ConnectionShutdown?.Invoke(sender, new ConnectionShutdownEventArgs(errorCode));
        }
    }

    public sealed class HttpClientConnection
    {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate void OnConnectionSetup(int errorCode);
            public delegate void OnConnectionShutdown(int errorCode);

            static private LibraryHandle library = new LibraryHandle();
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
        private HttpClientConnectionOptions options;

        public HttpClientConnection(HttpClientConnectionOptions options)
        {
            options.Validate();

            this.options = options;
            NativeHandle = API.make_new(
                options.ClientBootstrap.NativeHandle.DangerousGetHandle(),
                options.InitialWindowSize,
                options.HostName,
                options.Port,
                options.SocketOptions?.NativeHandle.DangerousGetHandle() ?? IntPtr.Zero,
                options.TlsConnectionOptions?.NativeHandle.DangerousGetHandle() ?? IntPtr.Zero,
                OnConnectionSetup,
                OnConnectionShutdown);
        }

        public HttpClientStream MakeRequest(HttpRequestOptions options)
        {
            return new HttpClientStream(this, options);
        }

        private void OnConnectionSetup(int errorCode)
        {
            options.OnConnectionSetup(this, errorCode);
        }

        private void OnConnectionShutdown(int errorCode)
        {
            options.OnConnectionShutdown(this, errorCode);
        }
    }
}
