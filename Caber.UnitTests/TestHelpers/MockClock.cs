using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caber.Util;

namespace Caber.UnitTests
{
    public class MockClock : IClock
    {
        public MockClock(DateTimeOffset initialNow)
        {
            now = initialNow;
        }

        private DateTimeOffset now;
        public DateTimeOffset Now
        {
            get
            {
                lock (pending)
                {
                    return now;
                }
            }
        }

        private readonly SortedList<DateTimeOffset, TaskCompletionSource<object>> pending = new SortedList<DateTimeOffset, TaskCompletionSource<object>>();

        private bool TryGetLastScheduledTime(out DateTimeOffset when)
        {
            lock (pending)
            {
                when = default;
                if (!pending.Any()) return false;
                when = pending.Keys.Last();
                return true;
            }
        }

        /// <summary>
        /// Advances the clock until all existing waits have completed.
        /// </summary>
        public void Advance()
        {
            if (!TryGetLastScheduledTime(out var when)) return;
            AdvanceTo(when);
        }

        public void Advance(TimeSpan interval)
        {
            AdvanceTo(Now + interval);
        }

        private void AdvanceTo(DateTimeOffset then)
        {
            if (then < Now) return; // Can't step backwards.

            while (DequeueNextTask(then, out var tcs))
            {
                try
                {
                    tcs.TrySetResult(null);
                }
                catch { }
            }
            lock (pending)
            {
                now = then;
            }
        }

        private bool DequeueNextTask(DateTimeOffset last, out TaskCompletionSource<object> tcs)
        {
            lock (pending)
            {
                tcs = default;
                if (!pending.Any()) return false;

                var slot = pending.First();
                if (slot.Key > last) return false;
                pending.Remove(slot.Key);
                tcs = slot.Value;
                return true;
            }
        }

        public Task WaitAsync(TimeSpan duration, CancellationToken token = default)
        {
            if (duration <= TimeSpan.Zero) return Task.CompletedTask;
            var deadline = Now + duration;
            return Task.WhenAny(GetOrCreate(deadline), token.AsTask());
        }

        private Task GetOrCreate(DateTimeOffset when)
        {
            lock (pending)
            {
                if (pending.TryGetValue(when, out var tcs)) return tcs.Task;
                var newTcs = new TaskCompletionSource<object>();
                pending[when] = newTcs;
                return newTcs.Task;
            }
        }
    }
}
