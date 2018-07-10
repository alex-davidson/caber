using System;
using System.ServiceProcess;

namespace Caber.Service
{
    internal class ServiceWrapper : ServiceBase
    {
        private readonly Service service;
        private IDisposable instance;

        public ServiceWrapper(Service service)
        {
            this.service = service;
        }

        protected override void OnStart(string[] args)
        {
            lock (service)
            {
                instance?.Dispose();
                instance = service.StartInstance();
            }
        }

        protected override void OnStop()
        {
            lock (service)
            {
                instance?.Dispose();
            }
        }
    }
}
