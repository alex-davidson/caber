using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.Retry;
using Caber.Util;

namespace Caber.Routing
{
    /// <summary>
    /// Deduplicated queue of FileChangeEvents.
    /// </summary>
    public class FileChangeEventQueue
    {
        /// <summary>
        /// Paths which are present in the queue.
        /// </summary>
        private readonly HashSet<AbstractPath> queuedPaths = new HashSet<AbstractPath>(default(AbstractPath.DefaultEqualityComparer));
        /// <summary>
        /// Paths which are pending queuing due to a retry token.
        /// </summary>
        private readonly Dictionary<AbstractPath, RetryToken> pendingPaths = new Dictionary<AbstractPath, RetryToken>(default(AbstractPath.DefaultEqualityComparer));
        private readonly Queue<FileChangeEvent> queue = new Queue<FileChangeEvent>();
        private readonly AutoResetEvent queued = new AutoResetEvent(false);
        public WaitHandle Queued => queued;

        public IClock Clock { get; set; } = Util.Clock.Default;

        public void Enqueue(FileChangeEvent fileChangeEvent)
        {
            lock (queue)
            {
                if (!queuedPaths.Add(fileChangeEvent.AbstractPath)) return;
                queue.Enqueue(fileChangeEvent);
                queued.Set();
            }
        }

        public void Enqueue(FileChangeEvent fileChangeEvent, RetryToken token)
        {
            if (token.HasExpired(Clock))
            {
                Enqueue(fileChangeEvent);
                return;
            }
            lock (queue)
            {
                pendingPaths.TryGetValue(fileChangeEvent.AbstractPath, out var existingToken);
                // If another operation already requested retry, use the earliest token.
                pendingPaths[fileChangeEvent.AbstractPath] = existingToken | token;
            }
            token.WaitAsync(Clock).ContinueWith(t => DeferredEnqueue(fileChangeEvent, token), TaskContinuationOptions.ExecuteSynchronously);
        }

        private void DeferredEnqueue(FileChangeEvent fileChangeEvent, RetryToken token)
        {
            lock (queue)
            {
                pendingPaths.TryGetValue(fileChangeEvent.AbstractPath, out var existingToken);
                if (!token.Equals(existingToken)) return;
                pendingPaths.Remove(fileChangeEvent.AbstractPath);
                if (queuedPaths.Add(fileChangeEvent.AbstractPath))
                {
                    queue.Enqueue(fileChangeEvent);
                }
                queued.Set();
            }
        }

        public bool TryDequeue(out FileChangeEvent fileChangeEvent)
        {
            fileChangeEvent = default;
            lock (queue)
            {
                while (queue.Any())
                {
                    fileChangeEvent = queue.Dequeue();
                    queuedPaths.Remove(fileChangeEvent.AbstractPath);
                    if (!pendingPaths.ContainsKey(fileChangeEvent.AbstractPath)) return true;
                    // If the path is pending, skip the enqueued instance.
                }
                return false;
            }
        }
    }
}
