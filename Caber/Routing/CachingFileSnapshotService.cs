using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.Util;

namespace Caber.Routing
{
    public class CachingFileSnapshotService : IFileSnapshotService
    {
        private readonly IFileSnapshotService inner;
        private readonly object syncObject = new object();
        /// <summary>
        /// Cache of snapshots.
        /// </summary>
        private readonly Dictionary<QualifiedPath, FileSnapshot> cache = new Dictionary<QualifiedPath, FileSnapshot>(default(QualifiedPath.DefaultEqualityComparer));
        /// <summary>
        /// Cache of pending requests.
        /// </summary>
        /// <remarks>
        /// The 'pending cache' aims to track only the snapshot requests currently in-flight.
        /// Tasks are rather heavyweight objects. They do possess a lot of fields but that's largely
        /// irrelevant: through continuations and captured lexical scopes they can retain large object
        /// graphs. So for a potentially-large cache of file snapshots we'd rather strip away the task
        /// as soon as it completes and cache just the snapshot.
        /// </remarks>
        private readonly Dictionary<QualifiedPath, PendingSnapshot> pendingCache = new Dictionary<QualifiedPath, PendingSnapshot>(default(QualifiedPath.DefaultEqualityComparer));

        public IClock Clock { get; set; } = Util.Clock.Default;

        public CachingFileSnapshotService(IFileSnapshotService inner)
        {
            this.inner = inner;
        }

        public Task<FileSnapshot> GetSnapshot(QualifiedPath qualifiedPath, DateTimeOffset minimumTimestamp)
        {
            lock (syncObject)
            {
                // Use cached entry, if sufficiently recent.
                if (cache.TryGetValue(qualifiedPath, out var existing))
                {
                    if (existing.Timestamp >= minimumTimestamp) return Task.FromResult(existing);
                }
                // Otherwise check for pending snapshots and endeavour to use them if possible.
                if (pendingCache.TryGetValue(qualifiedPath, out var pending))
                {
                    // If the request for the snapshot is sufficiently recent, use its eventual result.
                    if (pending.RequestedTimestamp >= minimumTimestamp) return pending.Task;
                    // Check if a result exists and promote it to the cache if so.
                    if (pending.Task.IsCompleted)
                    {
                        PromoteToCache(pending.Task.Result);
                        // If we have a result and it's sufficiently recent, use it.
                        if (pending.Task.Result.Timestamp >= minimumTimestamp) return pending.Task;
                    }
                }
                // No pending requests, or they're obsolete.
                return MakeNewRequest(qualifiedPath);
            }
        }

        public Stream ReadSnapshot(FileSnapshot snapshot)
        {
            return inner.ReadSnapshot(snapshot);
        }

        private Task<FileSnapshot> MakeNewRequest(QualifiedPath qualifiedPath)
        {
            var now = Clock.Now;
            var task = Task.Run(() => inner.GetSnapshot(qualifiedPath, now));
            var request = new PendingSnapshot(now, task);
            pendingCache[qualifiedPath] = request;
            request.Task.ContinueWith(OnRequestComplete, TaskContinuationOptions.OnlyOnRanToCompletion);
            return request.Task;
        }

        private void OnRequestComplete(Task<FileSnapshot> task)
        {
            lock (syncObject)
            {
                // If there is no pending request, bail out.
                if (!pendingCache.TryGetValue(task.Result.QualifiedPath, out var pending)) return;
                // If the pending request is not complete, bail out; it's not ours.
                if (!pending.Task.IsCompleted) return;

                PromoteToCache(pending.Task.Result);
                // After trying to promote the completed request, remove it from the pending list.
                pendingCache.Remove(task.Result.QualifiedPath);
            }
        }

        private bool PromoteToCache(FileSnapshot snapshot)
        {
            Debug.Assert(Monitor.IsEntered(syncObject));
            if (cache.TryGetValue(snapshot.QualifiedPath, out var existing))
            {
                if (existing.Timestamp >= snapshot.Timestamp) return false;
            }
            cache[snapshot.QualifiedPath] = snapshot;
            return true;
        }

        private struct PendingSnapshot
        {
            public PendingSnapshot(DateTimeOffset requestedTimestamp, Task<FileSnapshot> task)
            {
                RequestedTimestamp = requestedTimestamp;
                Task = task;
            }

            public DateTimeOffset RequestedTimestamp { get; }
            public Task<FileSnapshot> Task { get; }
        }
    }
}
