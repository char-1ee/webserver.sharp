using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace WebServer
{
    public static class Server
    {
        private static int maxSimultaneousConnections = 20;

        // A listening socket.
        private static HttpListener _listener;  

        // A semaphore that waits for a specified number of simul-allowed connections.
        private static Semaphore _semaphore = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);

        // Return list of IP addresses assigned to localhost network devices, 
        // including hardwired ethernet, wireless etc.
        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> res = host.AddressList.Where(
                    ip => ip.AddressFamily == AddressFamily.InterNetwork
                ).ToList();

            // res = GetIp4Address();

            return res;
        }

        // Instantiate HttpListener and add the localhost prefixes (subnet masks).
        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/");

            // Listen to IP address as well
            localhostIPs.ForEach(ip =>
            {
                Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
                listener.Prefixes.Add("http://" + ip.ToString() + "/");
            });

            return listener;
        }

        // Get localhost's IPv4 addresses
        private static IEnumerable<string> GetIp4Address()
        {
            List<string> res = new List<string>();

            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    res.Add(ip.ToString());
                }
            }
            return res;
        }

        // Begin listening to connections on a seperate worker thread.
        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener)); // invoke a worker thread
        }

        // Start awaiting for connections, up to the "maxSimultaneousConnections" value.
        // Running in a seperate thread.
        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                // blocking current thread until WaitHandle recevies a signal
                _semaphore.WaitOne(); 
                StartConnectionListener(listener);
            }
        }

        // Connection listener as an awaitable asynchronous process.
        private static async void StartConnectionListener(HttpListener listener)
        {
            // wait for a connection. Return to caller while we wait.
            HttpListenerContext context = await listener.GetContextAsync();

            // release the semaphore for another listener. _semaphore +1
            _semaphore.Release();

            // connection built now, processing
            string response = "Connected!";
            byte[] encoded = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
        }

        // Starts the web server.
        public static void Start()
        {
            List<IPAddress> localHostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localHostIPs);
            Start(listener);
        }
    }

}
