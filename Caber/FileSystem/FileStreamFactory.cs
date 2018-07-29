using System.IO;

namespace Caber.FileSystem
{
    public class FileStreamFactory
    {
        /// <summary>
        /// Read from the file, trying not to interfere with other processes' use of it.
        /// </summary>
        /// <remarks>
        /// FileShare.Delete is tricky: we don't expect to have logs deleted while we're
        /// still syncing them, and permitting other processes to delete the file from
        /// under us introduces a large number of possible additional failure modes,
        /// therefore we permit only reads and writes for now.
        /// </remarks>
        public Stream OpenForSilentRead(FileInfo fileInfo) => fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}
