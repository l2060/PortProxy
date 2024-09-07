using McMaster.Extensions.CommandLineUtils;

namespace PortProxy
{

    [Command(Name = "tpf", Description = "A TCP Port Forward Application"), Subcommand(typeof(AddCommand), typeof(RemoveCommand), typeof(StartCommand))]
    public class Program
    {
        public static int Main(string[] args)
        {
            if (OperatingSystem.IsWindows())
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            Console.Title = "Proxy(Tcp Port Forward) - By Hanks";
            return CommandLineApplication.Execute<Program>(args);
        }


        protected int OnExecute(CommandLineApplication app)
        {
            return 0;
        }

    }
}