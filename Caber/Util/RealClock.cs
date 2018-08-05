using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caber.Util
{
    public sealed class RealClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
        public Task WaitAsync(TimeSpan duration, CancellationToken token = default) => duration <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duration, token);
    }
}
