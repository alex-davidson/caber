using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using Caber.ConsoleSupport;

namespace Caber.Tool
{
    public class Program
    {
        private static int Main(string[] args)
        {
            var program = new Program();
            try
            {
                new ArgumentParser().Parse(args, program);
                return program.Run().GetAwaiter().GetResult();
            }
            catch (InvalidArgumentsException ex)
            {
                Console.Error.WriteLine(ex.Message);
                new ArgumentParser().WriteUsage(Console.Error);
                return 1;
            }
            catch (ApplicationException ex)
            {
                Console.Error.WriteLine(ex);
                return 3;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Cancelled.");
                return 255;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 4;
            }
        }

        public async Task<int> Run()
        {
            var cancelMonitor = new CancelKeyMonitor();
            cancelMonitor.LogRequestsTo(Console.Error);

            // Do stuff.

            return 0;
        }
    }
}
