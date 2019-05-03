using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Aws.Crt;
using Aws.Crt.IO;
using Aws.Crt.Http;

namespace Aws.Crt.Elasticurl
{
    class Context
    {
        // Args
        public LogLevel LogLevel { get; set; } = LogLevel.NONE;
        public string Verb { get; set; } = "GET";
        public string CACert { get; set; }
        public string CAPath { get; set; }
        public string Certificate { get; set; }
        public string PrivateKey { get; set; }
        public uint ConnectTimeoutMs { get; set; } = 3000;
        public List<string> Headers { get; set; } = new List<string>();
        public bool IncludeHeaders { get; set; } = false;
        public string OutputFilename { get; set; }
        public string TraceFile { get; set; }
        public bool Insecure { get; set; } = false;
        public Uri Uri { get; set; }

        // State
        public Stream OutputStream { get; set; }
        public Stream PayloadStream { get; set; }
    }

    class Elasticurl
    {
        static void ShowHelp()
        {
            Console.WriteLine("usage: elasticurl [options] url");
            Console.WriteLine(" url: url to make a request to. The default is a GET request.");
            Console.WriteLine("Options:");
            Console.WriteLine("      --cacert FILE: path to a CA certficate file.");
            Console.WriteLine("      --capath PATH: path to a directory containing CA files.");
            Console.WriteLine("  -c, --cert FILE: path to a PEM encoded certificate to use with mTLS");
            Console.WriteLine("      --key FILE: Path to a PEM encoded private key that matches cert.");
            Console.WriteLine("      --connect-timeout INT: time in milliseconds to wait for a connection.");
            Console.WriteLine("  -H, --header LINE: line to send as a header in format [header-key]: [header-value]");
            Console.WriteLine("  -d, --data STRING: Data to POST or PUT");
            Console.WriteLine("      --data-file FILE: File to read from file and POST or PUT");
            Console.WriteLine("  -M, --method STRING: Http Method verb to use for the request");
            Console.WriteLine("  -G, --get: uses GET for the verb.");
            Console.WriteLine("  -P, --post: uses POST for the verb.");
            Console.WriteLine("  -I, --head: uses HEAD for the verb.");
            Console.WriteLine("  -i, --include: includes headers in output.");
            Console.WriteLine("  -k, --insecure: turns off SSL/TLS validation.");
            Console.WriteLine("  -o, --output FILE: dumps content-body to FILE instead of stdout.");
            Console.WriteLine("  -t, --trace FILE: dumps logs to FILE instead of stderr.");
            Console.WriteLine("  -v, --verbose ERROR|INFO|DEBUG|TRACE: log level to configure. Default is none.");
            Console.WriteLine("  -h, --help: Display this message and quit.");
        }

        static string NextArg(string[] args, ref int argIdx)
        {
            if (args.Length > argIdx) 
            {
                return args[++argIdx];
            }
            throw new IndexOutOfRangeException(String.Format("Not enough arguments at position {0}", argIdx));
        }

        static void ParseArgs(string[] args) 
        {
            try 
            {
                // Convert --arg=value into --arg value
                List<string> expandedArgs = new List<string>();
                foreach (string arg in args) 
                {
                    if (arg.StartsWith("--"))
                    {
                        expandedArgs.AddRange(arg.Split("="));
                    }
                    else
                    {
                        expandedArgs.Add(arg);
                    }
                }
                args = expandedArgs.ToArray();
                // skip last arg, it should be the URI
                for (int argIdx = 0; argIdx < args.Length - 1; ++argIdx)
                {
                    string arg = args[argIdx];
                    switch (arg)
                    {
                        case "--cacert":
                            ctx.CACert = NextArg(args, ref argIdx);
                            break;
                        case "--capath":
                            ctx.CAPath = NextArg(args, ref argIdx);
                            break;
                        case "-c":
                        case "--cert":
                            ctx.Certificate = NextArg(args, ref argIdx);
                            break;
                        case "--data":
                        case "-d":
                            string data = NextArg(args, ref argIdx);
                            ctx.PayloadStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                            break;
                        case "--key":
                            ctx.PrivateKey = NextArg(args, ref argIdx);
                            break;
                        case "--connect-timeout":
                            ctx.ConnectTimeoutMs = uint.Parse(NextArg(args, ref argIdx));
                            break;
                        case "--get":
                        case "-G":
                            ctx.Verb = "GET";
                            break;
                        case "--data-file":
                            string file = NextArg(args, ref argIdx);
                            ctx.PayloadStream = File.OpenRead(file);
                            break;
                        case "--header":
                        case "-H":
                            ctx.Headers.Add(NextArg(args, ref argIdx));
                            break;
                        case "--help":
                        case "-h":
                            ShowHelp();
                            Environment.Exit(0);
                            break;
                        case "--head":
                        case "-I":
                            ctx.Verb = "HEAD";
                            break;
                        case "--include":
                        case "-i":
                            ctx.IncludeHeaders = true;
                            break;
                        case "--insecure":
                        case "-k":
                            ctx.Insecure = true;
                            break;
                        case "--method":
                        case "-M":
                            ctx.Verb = NextArg(args, ref argIdx);
                            break;
                        case "--output":
                        case "-o":
                            ctx.OutputFilename = NextArg(args, ref argIdx);
                            break;
                        case "--post":
                        case "-P":
                            ctx.Verb = "POST";
                            break;
                        case "--trace":
                        case "-t":
                            ctx.TraceFile = NextArg(args, ref argIdx);
                            break;
                        case "--verbose":
                        case "-v":
                            string level = NextArg(args, ref argIdx);
                            bool found = false;
                            foreach (LogLevel value in Enum.GetValues(typeof(LogLevel)))
                            {
                                if (string.Equals(level, value.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    ctx.LogLevel = value;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                Console.WriteLine("Invalid log level: {0}", level);
                                ShowHelp();
                                Environment.Exit(-1);
                            }
                            break;
                        default:
                            ShowHelp();
                            Environment.Exit(0);
                            return;
                    }
                }

                ctx.Uri = new Uri(args[args.Length - 1]);
            }
            catch (IndexOutOfRangeException)
            {
                ShowHelp();
                Environment.Exit(-1);
            }
            catch (UriFormatException ufe)
            {
                Console.WriteLine("Invalid URI: {0}: {1}", args[args.Length - 1], ufe.Message);
                Environment.Exit(-1);
            }
        }

        static void InitLogging()
        {
            Logger.EnableLogging(ctx.LogLevel, ctx.TraceFile);
        }

        static void InitOutput()
        {
            if (ctx.OutputFilename != null)
            {
                try 
                {
                    ctx.OutputStream = File.OpenWrite(ctx.OutputFilename);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to create/write to {0}: {1}", ctx.OutputFilename, ex);
                    Environment.Exit(-1);
                }
            }
            else
            {
                ctx.OutputStream = Console.OpenStandardOutput();
            }
        }

        static TlsConnectionOptions InitTls()
        {
            TlsConnectionOptions tlsConnectionOptions = null;
            if (ctx.Uri.Scheme == Uri.UriSchemeHttps || (ctx.Uri.Port != 80 && ctx.Uri.Port != 8080))
            {
                TlsContextOptions tlsOptions = null;
                if (ctx.Certificate != null && ctx.PrivateKey != null)
                {
                    try 
                    {
                        tlsOptions = TlsContextOptions.ClientMtlsFromPath(ctx.Certificate, ctx.PrivateKey);
                    }
                    catch (NativeException nex)
                    {
                        Console.WriteLine(
                            "Unable to initialize MTLS with cert {0} and key {1}: {2}", 
                            ctx.Certificate, ctx.PrivateKey, nex);
                        Environment.Exit(-1);
                    }
                }
                else
                {
                    tlsOptions = TlsContextOptions.DefaultClient();
                }

                if (ctx.CACert != null || ctx.CAPath != null)
                {
                    try
                    {
                        tlsOptions.OverrideDefaultTrustStoreFromPath(ctx.CAPath, ctx.CACert);
                    }
                    catch (NativeException nex)
                    {
                        Console.WriteLine("Unable to override default trust store: {0}", nex);
                        Environment.Exit(-1);
                    }
                }

                if (ctx.Insecure)
                {
                    tlsOptions.VerifyPeer = false;
                }

                tlsOptions.AlpnList = "http/1.1";

                try
                {
                    TlsContext tls = new ClientTlsContext(tlsOptions);
                    tlsConnectionOptions = new TlsConnectionOptions(tls);
                    tlsConnectionOptions.ServerName = ctx.Uri.Host;
                }
                catch (NativeException nex)
                {
                    Console.WriteLine("Unable to initialize TLS: {0}", nex);
                    Environment.Exit(-1);
                }
            }
            return tlsConnectionOptions;
        }

        static void OnConnectionSetup(int errorCode)
        {
            if (errorCode != 0)
            {
                var message = CRT.ErrorString(errorCode);
                _connectionSource.SetException(new WebException(String.Format("Failed to connect: {0}", message)));
            }
            else
            {
                _connectionSource.SetResult(_connection);
            }
        }

        static void OnConnectionShutdown(int errorCode)
        {
            Console.WriteLine("Disconnected");
        }

        private static HttpClientConnection _connection;
        private static TaskCompletionSource<HttpClientConnection> _connectionSource;
        static Task<HttpClientConnection> InitHttp(ClientBootstrap client, TlsConnectionOptions tlsOptions)
        {
            _connectionSource = new TaskCompletionSource<HttpClientConnection>();
            var options = new HttpClientConnectionOptions();
            options.ClientBootstrap = client;
            options.TlsConnectionOptions = tlsOptions;
            options.HostName = ctx.Uri.Host;
            options.Port = (UInt16)ctx.Uri.Port;
            options.OnConnectionSetup = OnConnectionSetup;
            options.OnConnectionShutdown = OnConnectionShutdown;
            if (ctx.ConnectTimeoutMs != 0)
            {
                var socketOptions = new SocketOptions();
                socketOptions.ConnectTimeoutMs = ctx.ConnectTimeoutMs;
                options.SocketOptions = socketOptions;
            }
            _connection = new HttpClientConnection(options);
            return _connectionSource.Task;
        }

        static bool responseCodeWritten = false;
        static void OnIncomingHeaders(HttpClientStream stream, HttpHeader[] headers)
        {
            if (ctx.IncludeHeaders)
            {
                if (!responseCodeWritten)
                {
                    Console.WriteLine("Response Status: {0}", stream.ResponseStatusCode);
                    responseCodeWritten = true;
                }

                foreach (var header in headers)
                {
                    Console.WriteLine("{0}:{1}", header.Name, header.Value);
                }
            }
        }

        static void OnIncomingBody(HttpClientStream stream, byte[] data, ref UInt64 windowSize)
        {
            ctx.OutputStream.Write(data, 0, data.Length);
        }

        static OutgoingBodyStreamState StreamOutgoingBody(HttpClientStream stream, byte[] buffer, out UInt64 bytesWritten)
        {
            bytesWritten = 0;
            if (ctx.PayloadStream != null)
            {
                var bufferStream = new MemoryStream(buffer);
                long prevPosition = ctx.PayloadStream.Position;
                ctx.PayloadStream.CopyTo(bufferStream, buffer.Length);
                bytesWritten = (UInt64)(ctx.PayloadStream.Position - prevPosition);
                if (ctx.PayloadStream.Position != ctx.PayloadStream.Length)
                {
                    return OutgoingBodyStreamState.InProgress;
                }
            }
            return OutgoingBodyStreamState.Done;
        }

        static void OnStreamComplete(HttpClientStream stream, int errorCode)
        {
            _streamSource.SetResult(errorCode);
        }
        
        private static TaskCompletionSource<int> _streamSource;
        private static HttpClientStream _stream;
        static Task<int> InitStream(HttpClientConnection connection)
        {
            _streamSource = new TaskCompletionSource<int>();

            var headers = new List<HttpHeader>();
            headers.Add(new HttpHeader("Host", ctx.Uri.Host));

            HttpRequestOptions options = new HttpRequestOptions();
            options.Method = ctx.Verb;
            options.Uri = ctx.Uri.PathAndQuery;
            options.Headers = headers.ToArray();
            options.OnIncomingHeaders = OnIncomingHeaders;
            options.OnIncomingBody = OnIncomingBody;
            options.OnStreamOutgoingBody = StreamOutgoingBody;
            options.OnStreamComplete = OnStreamComplete;
            _stream = connection.SendRequest(options);
            return _streamSource.Task;
        }

        private static Context ctx = new Context();
        static void Main(string[] args)
        {
            ParseArgs(args);
            InitLogging();
            InitOutput();

            var tlsOptions = InitTls();
            var elg = new EventLoopGroup();
            var client = new ClientBootstrap(elg);
            var connectionTask = InitHttp(client, tlsOptions);
            try 
            {
                var connection = connectionTask.Result;
                var streamTask = InitStream(connection);
                var result = streamTask.Result;
                if (ctx.OutputStream != null)
                {
                    ctx.OutputStream.Flush();
                    ctx.OutputStream.Close();
                }
                if (result != 0)
                {
                    Console.WriteLine("Stream failed: {0}", CRT.ErrorString(result));
                    Environment.Exit(-1);
                }
            }
            catch (AggregateException agg) // thrown by TaskCompletionSource
            {
                Console.Write("Operation failed: ");
                foreach (var ex in agg.Flatten().InnerExceptions)
                {
                    Console.WriteLine(ex.Message);
                }
                Environment.Exit(-1);
            }
        }
    }
}
