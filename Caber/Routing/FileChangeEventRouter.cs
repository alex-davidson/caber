using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.Logging;
using Caber.Retry;
using Caber.Util;

namespace Caber.Routing
{
    /// <summary>
    /// Processes file change events.
    /// </summary>
    public class FileChangeEventRouter : IWorkerLoop
    {
        private readonly FileChangeEventQueue queue;
        private readonly IFileSnapshotRoute[] senders;

        public IClock Clock { get; set; } = Util.Clock.Default;
        public IFileSnapshotService FileSnapshotService { get; set; }

        public FileChangeEventRouter(FileChangeEventQueue queue, IFileSnapshotService fileSnapshotService, IFileSnapshotRoute[] senders)
        {
            this.queue = queue;
            FileSnapshotService = fileSnapshotService;
            this.senders = senders.ToArray();
        }

        public Task WaitForWork(CancellationToken token) => queue.Queued.WaitOneAsync(token);

        public async Task RunOnce(CancellationToken token)
        {
            while (queue.TryDequeue(out var fileChangeEvent))
            {
                try
                {
                    var group = new RetryGroup();
                    var needed = senders.Where(s => s.Accepts(fileChangeEvent.AbstractPath)).ToArray();
                    if (!needed.Any()) continue;

                    var snapshot = await FileSnapshotService.GetSnapshot(fileChangeEvent.QualifiedPath, Clock.Now);
                    if (token.IsCancellationRequested) break;

                    await Task.WhenAll(needed.Select(s => s.HandleAsync(snapshot, fileChangeEvent.AbstractPath, group.Parallel(), token)).ToArray());
                    if (group.RetryRequested)
                    {
                        queue.Enqueue(fileChangeEvent, group.GetToken());
                    }
                }
                catch (Exception ex)
                {
                    Log.Operations.Error(new UnexpectedExceptionEvent(LogEventCategory.Routing, ex, fileChangeEvent.QualifiedPath.ToString()));
                }
                if (token.IsCancellationRequested) break;
            }
        }
    }
}
