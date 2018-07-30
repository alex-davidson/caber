using System;
using System.IO;
using System.Threading.Tasks;
using Caber.Util;

namespace Caber.FileSystem
{
    public interface IFileSnapshotService
    {
        Task<FileSnapshot> GetSnapshot(QualifiedPath qualifiedPath, DateTimeOffset effectiveTimestamp);
        Stream ReadSnapshot(FileSnapshot snapshot);
    }

    public class FileSnapshotService : IFileSnapshotService
    {
        private static readonly Task<FileSnapshot> NullTask = Task.FromResult<FileSnapshot>(null);
        private readonly StorageHierarchies storageHierarchies;

        public FileStreamFactory FileStreamFactory { get; set; } = new FileStreamFactory();
        public Sha256Hasher Sha256Hasher { get; set; } = new Sha256Hasher();

        public FileSnapshotService(StorageHierarchies storageHierarchies)
        {
            this.storageHierarchies = storageHierarchies;
        }

        public Task<FileSnapshot> GetSnapshot(QualifiedPath qualifiedPath, DateTimeOffset effectiveTimestamp)
        {
            var fileInfo = storageHierarchies.ResolveToFile(qualifiedPath);
            if (!fileInfo.Exists) return NullTask;
            var length = fileInfo.Length;
            try
            {
                using (var stream = OpenSnapshot(fileInfo, length))
                {
                    var sha256 = Sha256Hasher.Hash(stream);
                    return Task.FromResult(new FileSnapshot(effectiveTimestamp, qualifiedPath, length, sha256));
                }
            }
            catch (IOException)
            {
                fileInfo.Refresh();
                if (!fileInfo.Exists) return NullTask;
                throw;
            }
        }

        public Stream ReadSnapshot(FileSnapshot snapshot)
        {
            var fileInfo = storageHierarchies.ResolveToFile(snapshot.QualifiedPath);
            return OpenSnapshot(fileInfo, snapshot.Length);
        }

        private Stream OpenSnapshot(FileInfo fileInfo, long length)
        {
            var stream = FileStreamFactory.OpenForSilentRead(fileInfo);
            try
            {
                return new LengthLimitedReadOnlyStream(stream, length);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
    }
}
