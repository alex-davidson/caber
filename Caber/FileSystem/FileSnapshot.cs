using System;
using Caber.Util;

namespace Caber.FileSystem
{
    /// <summary>
    /// Point-in-time record of a file's contents.
    /// </summary>
    public class FileSnapshot
    {
        public FileSnapshot(DateTimeOffset timestamp, QualifiedPath qualifiedPath, long length, Hash sha256)
        {
            Timestamp = timestamp;
            QualifiedPath = qualifiedPath;
            Length = length;
            Sha256 = sha256;
        }

        public DateTimeOffset Timestamp { get; set; }
        public QualifiedPath QualifiedPath { get; set; }
        public long Length { get; set; }
        public Hash Sha256 { get; set; }
    }
}
