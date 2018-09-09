using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caber.Util
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
        Task WaitAsync(TimeSpan duration, CancellationToken token = default);
    }
}
