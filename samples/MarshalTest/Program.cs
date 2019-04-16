using System;

using Aws.Crt.IO;

namespace MarshalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("HELLO WORLD");
            TlsContextOptions options = new TlsContextOptions();
            options.AlpnList = "h2;x-amazon-mqtt";
        }
    }
}
