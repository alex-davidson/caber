using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caber.Util;

namespace Caber.UnitTests.TestHelpers
{
    public class ConcurrentOperations
    {
        /// <summary>
        /// Run the specified action at a requested degree of concurrency.
        /// </summary>
        /// <remarks>
        /// Waits for all threads to spin up before running the action. Completes when
        /// all instances of the action have completed.
        /// Uses Threads rather than the threadpool in an attempt to force concurrent
        /// execution, although in practice this will be limited by available CPUs and
        /// system workload.
        /// </remarks>
        public static async Task Run(int count, Action action)
        {
            var start = new Barrier(count);
            var end = new CountdownEvent(count);
            var threads = Enumerable.Range(0, count).Select(_ => RunThread(start, end, action)).ToArray();
            await end.WaitHandle.WaitOneAsync(CancellationToken.None).ConfigureAwait(false);
            GC.KeepAlive(threads);
        }

        private static Thread RunThread(Barrier start, CountdownEvent end, Action action)
        {
            var thread = new Thread(_ => {
                start.SignalAndWait();
                try
                {
                    action();
                }
                finally
                {
                    end.Signal();
                }
            });
            thread.Start();
            return thread;
        }
    }
}
