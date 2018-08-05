using System;
using System.Threading;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.Logging;
using Caber.Util;

namespace Caber.Routing
{
    /// <summary>
    /// Translates file paths from the source pool, maps them to the internal model, and queues the ones which survive filtering.
    /// </summary>
    public class FileChangeEventTranslator : IWorkerLoop
    {
        private readonly FileSystemPathPool sourcePool;
        private readonly StorageHierarchies storageHierarchies;
        private readonly FileChangeEventQueue queue;
        private readonly IDiagnosticsLog diagnosticsLog;

        public IClock Clock { get; set; } = Util.Clock.Default;

        public FileChangeEventTranslator(FileSystemPathPool sourcePool, StorageHierarchies storageHierarchies, FileChangeEventQueue queue, IDiagnosticsLog diagnosticsLog = null)
        {
            this.sourcePool = sourcePool;
            this.storageHierarchies = storageHierarchies;
            this.queue = queue;
            this.diagnosticsLog = diagnosticsLog;
        }

        public Task WaitForWork(CancellationToken token) => sourcePool.Queued.WaitOneAsync(token);

        public Task RunOnce(CancellationToken token)
        {
            while (sourcePool.TryTake(out var path))
            {
                try
                {
                    if (!storageHierarchies.TryResolveQualifiedPath(path, out var qualifiedPath)) continue;
                    var abstractPath = storageHierarchies.MapToAbstractPath(qualifiedPath, diagnosticsLog);
                    if (abstractPath == null) continue;

                    queue.Enqueue(new FileChangeEvent(Clock.Now, qualifiedPath, abstractPath));
                }
                catch (Exception ex)
                {
                    Log.Operations.Error(new UnexpectedExceptionEvent(LogEventCategory.Routing, ex, path));
                }
                if (token.IsCancellationRequested) break;
            }
            return Task.CompletedTask;
        }
    }
}
