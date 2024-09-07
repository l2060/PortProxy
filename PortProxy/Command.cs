using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace PortProxy
{
    [Command("add", Description = "Add a remote TCP endpoint to the local port proxy")]
    class AddCommand
    {
        [Argument(0, "port", "Local listening port number")]
        public string? localPort { get; set; }

        [Argument(1, "endPoint", "Remote TCP endpoint (192.168.1.100:8000)")]
        public string? endPoint { get; set; }

        protected int OnExecute(CommandLineApplication app)
        {
            var port = UInt16.Parse(localPort);
            var lines = File.ReadAllLines("conf.txt").Select(e => e.Trim()).Where(e => e.Length > 0 && !e.StartsWith("#"));
            if (lines.Where(e => e.StartsWith("0.0.0.0:" + port)).Count() > 0)
            {
                Console.WriteLine($"The local port[{port}] is occupied. Procedure");
                return 1;
            }
            IPEndPoint.Parse(endPoint);
            Console.WriteLine($"Added: 0.0.0.0:{localPort} => {endPoint}");
            File.AppendAllLines("conf.txt", new List<String>() { "", $"0.0.0.0:{localPort} {endPoint}", "" });
            return 0;
        }
    }




    [Command("remove", Description = "Remove a remote TCP endpoint to the local port proxy")]
    class RemoveCommand
    {
        [Argument(0, "port", "Local listening port number")]
        public string? localPort { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            var port = UInt16.Parse(localPort);
            var lines = File.ReadAllLines("conf.txt").Select(e => e.Trim()).Where(e => !e.StartsWith($"0.0.0.0:{port}"));
            Console.WriteLine($"Removed: 0.0.0.0:{port}");
            File.WriteAllLines("conf.txt", lines);
            return 0;
        }
    }




    [Command("start", Description = "Remove a remote TCP endpoint to the local port proxy")]
    class StartCommand
    {
        private static List<ProxyClient> clients = new List<ProxyClient>();


        protected int OnExecute(CommandLineApplication app)
        {
            Console.Clear();
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
            Console.CursorVisible = false;
            Timer timer = new Timer(refresh, 0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
            new HostBuilder().UseConsoleLifetime().Build().Run();
            timer.Dispose();
            Console.CursorVisible = true;
            return 0;
        }


        private void refresh(Object? state)
        {

            Console.Write("\u001b[H");
            Console.WriteLine("###################################################################################################################");
            Console.Write("{0, -18}", "LOCAL ENDPOINT");
            Console.Write("{0, -22}", "REMOTE ENDPOINT");
            Console.Write("{0, -10}", "CLIENTS");
            Console.Write("{0, -10}", "ERRORS");
            Console.Write("{0, -12}", "UP/s");
            Console.Write("{0, -12}", "DOWN/s");
            Console.Write("{0, -12}", "UP TOTAL");
            Console.Write("{0, -12}", "DOWN TOTAL");
            Console.WriteLine("{0, -12}", "STATUS");
            Console.WriteLine("###################################################################################################################");


            foreach (var client in clients)
            {
                Console.Write("{0, -18}", client.LocalEndPoint.ToString().PadRight(18));   // 确保每次写入相同长度
                Console.Write("{0, -22}", client.RemoteEndPoint.ToString().PadRight(22)); // 通过 PadRight 补齐空白字符
                Console.Write("{0, -10}", client.ClientNum.ToString().PadRight(10));
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0, -10}", client.ErrorNum.ToString().PadRight(10));
                Console.ForegroundColor = color;
                Console.Write("{0, -12}", client.UpFlowSecond.ToString().PadRight(12));
                Console.Write("{0, -12}", client.DownFlowSecond.ToString().PadRight(12));
                Console.Write("{0, -12}", client.UpFlowTotal.ToString().PadRight(12));
                Console.Write("{0, -12}", client.DownFlowTotal.ToString().PadRight(12));
                Console.WriteLine(client.Status.PadRight(12));  // 确保状态也被正确覆盖
                client.ResetFlow();
            }
            Console.WriteLine();
        }





    }





}
