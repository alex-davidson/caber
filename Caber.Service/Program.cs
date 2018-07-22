using System;
using System.ServiceProcess;
using System.Text;
using Caber.ConsoleSupport;

namespace Caber.Service
{
    public class Program
    {
        private static int Main(string[] args)
        {
            if (!IsSTDERRAvailable())
            {
                return RunAsService();
            }
            return RunAsConsoleApp(args);
        }

        private static int RunAsConsoleApp(string[] args)
        {
            var service = new Service();
            try
            {
                new ArgumentParser().Parse(args, service);
                var program = new ConsoleWrapper(service);
                return program.Run();
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

        private static int RunAsService()
        {
            var service = new Service();
            ServiceBase.Run(new ServiceWrapper(service));
            return 0;
        }

        private static bool IsSTDERRAvailable()
        {
            using (var stderr = Console.OpenStandardError())
            {
                if (stderr == null) return false;
                if (!stderr.CanWrite) return false;
                if (stderr.GetType().FullName == "System.IO.Stream+NullStream") return false;
            }
            return true;
        }
    }
}
