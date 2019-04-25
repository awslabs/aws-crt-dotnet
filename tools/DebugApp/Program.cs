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

            var promise = new TaskCompletionSource<HttpClientConnection>();
            HttpClientConnection connection = null;
            var options = new HttpClientConnectionOptions();
            options.ClientBootstrap = clientBootstrap;
            options.HostName = "www.amazon.com";
            options.Port = 80;
            options.OnConnectionSetup = (int errorCode) =>
            {
                Console.WriteLine("CONNECTED");
                promise.SetResult(connection);
            };
            options.OnConnectionShutdown = (int errorCode) =>
            {
                Console.WriteLine("DISCONNECTED");
            };
            options.TlsConnectionOptions = tlsConnectionOptions;
            connection = new HttpClientConnection(options);
            CreateStream(promise.Task.Result);
            Console.WriteLine("DONE");
        }

        internal struct VoidTaskResult 
        {
            public static readonly VoidTaskResult Value = default(VoidTaskResult);
        }
        static void CreateStream(HttpClientConnection connection)
        {
            Console.WriteLine("NEW STREAM");
            var promise = new TaskCompletionSource<VoidTaskResult>();
            HttpRequestOptions streamOptions = new HttpRequestOptions();
            streamOptions.Method = "GET";
            streamOptions.Uri = URI;
            streamOptions.Headers = new HttpHeader[] {
                new HttpHeader("Test-Header", "Test-Value"),
                new HttpHeader("Additional-Header", "Additional-Value")
            };
            streamOptions.OnIncomingHeaders = (s, headers) =>
            {
                foreach (var header in headers) {
                    Console.WriteLine("HEADER: {0}: {1}", header.Name, header.Value);
                }
            };
            streamOptions.OnIncomingHeaderBlockDone = (s, hasBody) => {
                Console.WriteLine("HEADERS DONE, {0}", hasBody ? "EXPECTING BODY" : "NO BODY");   
            };
            streamOptions.OnIncomingBody = (s, data) => {
                Console.WriteLine("BODY: (size={0})", data.Length);
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(data));
            };
            streamOptions.OnStreamComplete = (s, errorCode) =>
            {
                Console.WriteLine("COMPLETE: {0}", errorCode);
                promise.SetResult(VoidTaskResult.Value);
            };
            var stream = new HttpClientStream(connection, streamOptions);
            promise.Task.Wait();
        }
    }
}
