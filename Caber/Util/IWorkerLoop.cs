using System.Threading;
using System.Threading.Tasks;

namespace Caber.Util
{
    public interface IWorkerLoop
    {
        Task WaitForWork(CancellationToken token = default);
        Task RunOnce(CancellationToken token = default);
    }
}
