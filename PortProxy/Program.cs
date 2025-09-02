using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace PortProxy
{
    public class Program
    {
        static int Main(string[] args)
        {
            // root
            var rootCommand = new RootCommand("端口转发管理工具");

            // start
            var startCommand = new Command("--start", "启动服务");
            startCommand.SetAction(parseResult =>
            {
                Start();
                return 0;
            });


            // list
            var listCommand = new Command("--list", "列出所有记录");
            listCommand.SetAction(parseResult =>
            {
                var lines = File.ReadAllLines("conf.txt");
                foreach (var item in lines)
                {
                    if (!String.IsNullOrWhiteSpace(item) && !item.StartsWith("#"))
                    {
                        Console.WriteLine(item);
                    }
                }
                return 0;
            });



            // add
            var addCommand = new Command("--add", "添加端口转发规则");
            var localPortArgument = new Argument<int>("localPort") { Description = "本地端口" };
            var remoteHostArgument = new Argument<String>("remoteHost") { Description = "远程主机" };
            var remotePortArgument = new Argument<int>("remotePort") { Description = "远程端口" };
            addCommand.Arguments.Add(localPortArgument);
            addCommand.Arguments.Add(remoteHostArgument);
            addCommand.Arguments.Add(remotePortArgument);
            addCommand.SetAction(parseResult =>
            {
                var localPort = parseResult.GetValue(localPortArgument);
                var remoteHost = parseResult.GetValue(remoteHostArgument);
                var remotePort = parseResult.GetValue(remotePortArgument);
                add2Conf(localPort, remoteHost, remotePort);
                return 0;
            });

            // remove
            var rmlocalPortArgument = new Argument<int>("localPort") { Description = "要删除的本地端口" };
            var removeCommand = new Command("--remove", "删除端口转发规则");
            removeCommand.Add(rmlocalPortArgument);
            removeCommand.SetAction(parseResult =>
            {
                var localPort = parseResult.GetValue(rmlocalPortArgument);
                removeConf(localPort);
                return 0;
            });

            // 添加子命令
            rootCommand.Add(listCommand);
            rootCommand.Add(startCommand);
            rootCommand.Add(addCommand);
            rootCommand.Add(removeCommand);
            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }






        private static void add2Conf(int localPort, String endHost, int endPort)
        {
            var port = localPort;
            var lines = File.ReadAllLines("conf.txt").Select(e => e.Trim()).Where(e => e.Length > 0 && !e.StartsWith("#"));
            if (lines.Where(e => e.StartsWith("0.0.0.0:" + port)).Count() > 0)
            {
                Console.WriteLine($"The local port[{port}] is occupied. Procedure");
                return;
            }
            var endPoint = endHost + ":" + endPort;
            IPEndPoint.Parse(endPoint);
            Console.WriteLine($"Added: 0.0.0.0:{localPort} => {endPoint}");
            File.AppendAllLines("conf.txt", new List<String>() { "", $"0.0.0.0:{localPort} {endPoint}", "" });
        }


        private static void removeConf(int localPort)
        {
            var port = localPort;
            var lines = File.ReadAllLines("conf.txt").Select(e => e.Trim()).Where(e => !e.StartsWith($"0.0.0.0:{port}"));
            Console.WriteLine($"Removed: 0.0.0.0:{port}");
            File.WriteAllLines("conf.txt", lines);
        }



        private static void Start()
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

        }


        private static List<ProxyClient> clients = new List<ProxyClient>();




        private static void refresh(Object? state)
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