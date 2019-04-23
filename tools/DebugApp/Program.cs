using System;

using Aws.Crt.IO;
using Aws.Crt.Http;

// Any code you put in Main() can be debugged natively by running
// dotnet exec /path/to/DebugApp/bin/Debug/netcoreapp2.1/DebugApp.dll
namespace DebugApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("HELLO WORLD");
            var elg = new EventLoopGroup(1);
            var clientBootstrap = new ClientBootstrap(elg);
            var options = new HttpClientConnectionOptions();
            options.ClientBootstrap = clientBootstrap;
            options.HostName = "www.amazon.com";
            options.Port = 80;
            options.OnConnectionSetup = (int errorCode) =>
            {
                Console.WriteLine("CONNECTED");
            };
            options.OnConnectionShutdown = (int errorCode) =>
            {
                Console.WriteLine("DISCONNECTED");
            };
            var connection = new HttpClientConnection(options);
        }
    }
}
