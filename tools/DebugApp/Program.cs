using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

using Aws.Crt.IO;
using Aws.Crt.Http;

// Any code you put in Main() can be debugged natively by running
// dotnet exec /path/to/DebugApp/bin/Debug/netcoreapp2.1/DebugApp.dll
namespace DebugApp
{
    class Program
    {
        static readonly Uri URI = new Uri("https://aws-crt-test-stuff.s3.amazonaws.com/http_test_doc.txt");

        static void Main(string[] args)
        {
            Console.WriteLine("HELLO WORLD");
            var elg = new EventLoopGroup(1);
            var clientBootstrap = new ClientBootstrap(elg);

            var tlsCtxOptions = TlsContextOptions.DefaultClient();
            var tlsContext = new ClientTlsContext(tlsCtxOptions);
            var tlsConnectionOptions = new TlsConnectionOptions(tlsContext);
            tlsConnectionOptions.ServerName = URI.Host;

            var promise = new TaskCompletionSource<int>();
            HttpClientConnection connection = null;
            var options = new HttpClientConnectionOptions();
            options.ClientBootstrap = clientBootstrap;
            options.HostName = URI.Host;
            options.Port = (UInt16)URI.Port;
            options.OnConnectionSetup = (int errorCode) =>
            {
                Console.WriteLine(errorCode == 0 ? "CONNECTED" : "FAILED");
                promise.SetResult(errorCode);
            };
            options.OnConnectionShutdown = (int errorCode) =>
            {
                Console.WriteLine("DISCONNECTED");
            };
            options.TlsConnectionOptions = (URI.Scheme == "https") ? tlsConnectionOptions : null;
            connection = new HttpClientConnection(options);

            if (promise.Task.Result == 0)
            {
                CreateStream(connection);
            }
            Console.WriteLine("DONE");
        }

        internal struct VoidTaskResult 
        {
            public static readonly VoidTaskResult Value = default(VoidTaskResult);
        }
        static void CreateStream(HttpClientConnection connection)
        {
            Console.WriteLine("NEW STREAM");
            int totalSize = 0;
            var promise = new TaskCompletionSource<VoidTaskResult>();
            HttpRequestOptions streamOptions = new HttpRequestOptions();
            streamOptions.Method = "GET";
            streamOptions.Uri = URI.PathAndQuery;
            streamOptions.Headers = new HttpHeader[] {
                new HttpHeader("Host", URI.Host),
                new HttpHeader("Content-Length", "42")
            };
            streamOptions.OnIncomingHeaders = (s, headers) =>
            {
                Console.WriteLine("RESPONSE: {0}", s.ResponseStatusCode);
                foreach (var header in headers) {
                    Console.WriteLine("HEADER: {0}: {1}", header.Name, header.Value);
                }
            };
            streamOptions.OnIncomingHeaderBlockDone = (s, hasBody) => {
                Console.WriteLine("HEADERS DONE, {0}", hasBody ? "EXPECTING BODY" : "NO BODY");   
            };
            streamOptions.OnIncomingBody = (s, data) => {
                totalSize += data.Length;
                Console.WriteLine("BODY CHUNK: (size={0})", data.Length);
            };
            streamOptions.OnStreamOutgoingBody = (HttpClientStream s, byte[] buffer, out UInt64 bytesWritten) => {
                buffer[0] = (byte)'Z';
                bytesWritten = 1;
                return OutgoingBodyStreamState.Done;
            };
            streamOptions.OnStreamComplete = (s, errorCode) =>
            {
                Console.WriteLine("COMPLETE: rc={0}, total body size={1}", errorCode, totalSize);
                promise.SetResult(VoidTaskResult.Value);
            };
            var stream = connection.SendRequest(streamOptions);
            promise.Task.Wait();
        }
    }
}
