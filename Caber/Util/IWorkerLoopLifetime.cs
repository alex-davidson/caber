using System;
using System.Threading.Tasks;

namespace Caber.Util
{
    public interface IWorkerLoopLifetime : IDisposable
    {
        Task StopAsync();
    }
}
