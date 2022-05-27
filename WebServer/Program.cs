using System;

using WebServer;

namespace ConsoleWebServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server.Start();
            Console.ReadLine();
        }
    }
}