using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace WebServer
{
    public static class Server
    {
        public enum ServerError
        {
            OK,
            ExpiredSession,
            NotAuthorized,
            FileNotFound,
            PageNotFound,
            ServerError,
            UnknownType,
            ValidationError,
            AjaxError,
        }

        private static HttpListener _listener;
        private static int maxSimulaneousConnections = 20;
        private static Semaphore _semaphore = new Semaphore(maxSimulaneousConnections, maxSimulaneousConnections);

        /// <summary>
        /// Returns list of IP addresses assigned to localhost network devices.
        /// </summary>
        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> res = new List<IPAddress>();
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    res.Add(ip);
                }
            }
            return res;
        }

        /// <summary>
        /// Initialize and return a HttpListener according to localhost IPs.
        /// </summary>
        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/");

            // Listen to all IP addresses in localhost
            localhostIPs.ForEach(ip =>
            {
                Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
                listener.Prefixes.Add("http://" + ip.ToString() + "/");
            });

            return listener;
        }

        /// <summary>
        /// Internal Start() on a seperate worker thread.
        /// </summary>
        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        /// <summary>
        /// Start awaiting for connections, up to the maxSimultaneousConnections value.
        /// </summary>
        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                _semaphore.WaitOne();
                StartConnectionListener(listener);
            }
        }

        /// <summary>
        /// Await connections.
        /// </summary>
        private static async void StartConnectionListener(HttpListener listener)
        {
            HttpListenerContext context = await listener.GetContextAsync();

            _semaphore.Release();
            
            // after connections built, give a response
            string response = "Connection built successfully!";
            byte[] encoded = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
        }

        public static void Start()
        {
            List<IPAddress> localhostIps = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localhostIps);
            Start(listener);
        }
    }
}
