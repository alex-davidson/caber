using System;
using System.Threading;
using System.Threading.Tasks;
using Caber.Logging;

namespace Caber.Util
{
    public class WorkerRunner
    {
        public IWorkerLoopLifetime Run(IWorkerLoop loop, TaskScheduler scheduler = null)
        {
            var instance = new WorkerLoopLifetime(loop, scheduler ?? TaskScheduler.Default);
            instance.Start();
            return instance;
        }

        private class WorkerLoopLifetime : IWorkerLoopLifetime
        {
            private readonly IWorkerLoop loop;
            private readonly TaskScheduler scheduler;
            private readonly CancellationTokenSource shutdownCts = new CancellationTokenSource();
            private Task running;

            public WorkerLoopLifetime(IWorkerLoop loop, TaskScheduler scheduler)
            {
                this.loop = loop ?? throw new ArgumentNullException(nameof(loop));
                this.scheduler = scheduler;
            }

            public void Start()
            {
                lock (this)
                {
                    if (running != null) throw new InvalidOperationException("Loop is already running.");
                    running = Task.Factory.StartNew(RunInternal, shutdownCts.Token, TaskCreationOptions.None, scheduler);
                }
            }

            public Task StopAsync()
            {
                shutdownCts.Cancel();
                lock (this)
                {
                    if (running == null) throw new InvalidOperationException("Loop is not running.");
                }
                return running;
            }

            private async Task RunInternal()
            {
                try
                {
                    while (!shutdownCts.IsCancellationRequested)
                    {
                        try
                        {
                            await loop.RunOnce(shutdownCts.Token);
                            await loop.WaitForWork(shutdownCts.Token);
                        }
                        catch (OperationCanceledException ex)
                        {
                            if (ex.CancellationToken == shutdownCts.Token) return;
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Operations.Error(new UnexpectedExceptionEvent(LogEventCategory.None, ex, loop.GetType().FullName));
                }
            }

            public void Dispose()
            {
                shutdownCts.Cancel();
            }
        }
    }
}
