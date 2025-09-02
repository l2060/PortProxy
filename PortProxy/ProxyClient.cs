using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
namespace PortProxy
{

    internal class SizeNumber
    {

        public SizeNumber(long value)
        {
            _value = value;
        }


        private long _value = 0;
        private string[] NUMBER_FORMAT_LEVELS = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };



        public void Reset()
        {
            _value = 0;
        }


        public void Add(long value)
        {
            //Interlocked.Add
            _value += value;
        }



        public override string ToString()
        {
            int counter = 0;
            double number = _value;
            int maxCount = NUMBER_FORMAT_LEVELS.Length - 1;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
                if (counter >= maxCount)
                {
                    break;
                }
            }
            return $"{string.Format("{0:0.00}", number)}{NUMBER_FORMAT_LEVELS[counter]}";
        }

        public string Format(long bytes, string formatString = "{0:0.00}")
        {
            int counter = 0;
            double number = bytes;
            int maxCount = NUMBER_FORMAT_LEVELS.Length - 1;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
                if (counter >= maxCount)
                {
                    break;
                }
            }
            return $"{string.Format(formatString, number)}{NUMBER_FORMAT_LEVELS[counter]}";
        }
    }










    internal class ProxyClient
    {
        private CancellationTokenSource cancelSource { get; set; }
        private TcpListener listener { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public Int32 BufferSize { get; set; } = 81920;

        public Int32 ClientNum = 0;
        public Int32 ErrorNum = 0;

        public String Status = "";

        public SizeNumber UpFlowTotal = new SizeNumber(0);
        public SizeNumber DownFlowTotal = new SizeNumber(0);

        public SizeNumber UpFlowSecond = new SizeNumber(0);
        public SizeNumber DownFlowSecond = new SizeNumber(0);


        public ProxyClient(IPEndPoint localE, IPEndPoint remoteE)
        {
            cancelSource = new CancellationTokenSource();
            this.LocalEndPoint = localE;
            this.RemoteEndPoint = remoteE;
        }

        public void ResetFlow()
        {
            UpFlowSecond.Reset();
            DownFlowSecond.Reset();
        }


        public void Stop()
        {
            cancelSource.Cancel();
            this.Status = "Stop";
        }



        public void Start()
        {
            try
            {
                cancelSource.TryReset();
                listener = new TcpListener(LocalEndPoint);
                listener.Start();
                Task.Run(AcceptClients, cancelSource.Token);
                this.Status = "Running";
            }
            catch (Exception ex)
            {
                this.Status = "Error " + ex.Message;
            }
        }


        private async Task AcceptClients()
        {
            while (!cancelSource.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cancelSource.Token);
                var task = Task.Run(() => HandleConnectionAsync(client), cancelSource.Token);
            }
        }





        private async Task HandleConnectionAsync(TcpClient client)
        {
            var remote = new TcpClient();
            try
            {
                Interlocked.Increment(ref ClientNum);
                await remote.ConnectAsync(RemoteEndPoint);

                NetworkStream clientStream = client.GetStream();
                NetworkStream forwardStream = remote.GetStream();
                Task copyToForward = CopyToAsync(clientStream, forwardStream, (f) =>
                {
                    UpFlowTotal.Add(f);
                    UpFlowSecond.Add(f);
                });
                Task copyToClient = CopyToAsync(forwardStream, clientStream, (f) =>
                {
                    DownFlowTotal.Add(f);
                    DownFlowSecond.Add(f);
                });
                await Task.WhenAny(copyToForward, copyToClient);
                remote.Close();
            }
            catch (SocketException ex)
            {
                Interlocked.Increment(ref ErrorNum);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                client.Close();
                Interlocked.Decrement(ref ClientNum);
            }
        }


        private async Task CopyToAsync(Stream source, Stream destination, Action<int> flowInc)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancelSource.Token).ConfigureAwait(false)) != 0)
                {
                    flowInc(bytesRead);
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancelSource.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

    }
}
