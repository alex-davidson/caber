using System;
using Caber.ConsoleSupport;

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
