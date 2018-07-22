using System;
using Caber.ConsoleSupport;
using Caber.Logging;

namespace Caber.Service
{
    internal class ConsoleWrapper
    {
        private readonly Service service;

        public ConsoleWrapper(Service service)
        {
            this.service = service;
        }

        public int Run()
        {
            Log.Configure(new ConsoleLoggingPolicy());
            var cancelMonitor = new CancelKeyMonitor();
            cancelMonitor.LogRequestsTo(Console.Error);

            using (service.StartInstance())
            {
                cancelMonitor.WaitForCancel();
            }

            return 0;
        }
    }
}
