using System.Threading;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.Retry;

namespace Caber.Routing
{
    public interface IFileSnapshotRoute
    {
        /// <summary>
        /// Indicates whether the specified abstract path is replicated via this route.
        /// </summary>
        bool Accepts(AbstractPath abstractPath);
        /// <summary>
        /// Indicates whether the route has met its replication target for the specified snapshot as the specified abstract path.
        /// </summary>
        /// <remarks>
        /// If the route has multiple senders, this should return 'true' if it has met its replication target.
        /// </remarks>
        bool IsHandled(FileSnapshot snapshot, AbstractPath abstractPath);
        /// <summary>
        /// Replicates the specified snapshot as the specified abstract path. Returns true if the destination is up to date.
        /// </summary>
        /// <remarks>
        /// If the route has multiple senders it should attempt to send to all of them.
        /// </remarks>
        Task<RetryToken> HandleAsync(FileSnapshot snapshot, AbstractPath abstractPath, IRetryCollector retryCollector = null, CancellationToken token = default);
    }
}
