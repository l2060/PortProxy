using System.Net;


namespace PortProxy
{
    public class Program
    {

        private static List<ProxyClient> clients = new List<ProxyClient>();


        public static void Main(string[] args)
        {
            var lines = File.ReadAllLines("conf.txt")
                 .Select(e => e.Trim())
                 .Where(e => e.Length > 0 && !e.StartsWith("#"));

            foreach (var line in lines)
            {
                var sp = line.Split(' ');
                var local = IPEndPoint.Parse(sp[0]);
                var remote = IPEndPoint.Parse(sp[1]);
                var client = new ProxyClient(local, remote);
                client.Start();
                clients.Add(client);
            }


            while (true)
            {
                Console.Clear();
                Console.Write("{0, -22}", "LOCAL ENDPOINT");
                Console.Write("{0, -22}", "REMOTE ENDPOINT");
                Console.Write("{0, -10}", "CLIENTS");

                Console.Write("{0, -12}", "UP/s");
                Console.Write("{0, -12}", "DOWN/s");

                Console.Write("{0, -12}", "UP TOTAL");
                Console.Write("{0, -12}", "DOWN TOTAL");
                Console.WriteLine("STATUS");

                foreach (var client in clients)
                {
                    Console.Write("{0, -22}", client.LocalEndPoint);
                    Console.Write("{0, -22}", client.RemoteEndPoint);
                    Console.Write("{0, -10}", client.ClientNum);
                    Console.Write("{0, -12}", client.UpFlowSecond);
                    Console.Write("{0, -12}", client.DownFlowSecond);
                    Console.Write("{0, -12}", client.UpFlowTotal);
                    Console.Write("{0, -12}", client.DownFlowTotal);
                    Console.WriteLine(client.Status);
                    client.ResetFlow();
                }
                
                Thread.Sleep(1000);
            }
        }
    }
}