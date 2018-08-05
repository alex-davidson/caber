using System;
using System.Collections.Generic;
using Caber.FileSystem;

namespace Caber.Routing
{
    public struct FileChangeEvent
    {
        public FileChangeEvent(DateTimeOffset timestamp, QualifiedPath qualifiedPath, AbstractPath abstractPath)
        {
            Timestamp = timestamp;
            QualifiedPath = qualifiedPath;
            AbstractPath = abstractPath;
        }

        public DateTimeOffset Timestamp { get; }
        public QualifiedPath QualifiedPath { get; }
        public AbstractPath AbstractPath { get; }

        public struct AbstractPathEqualityComparer : IEqualityComparer<FileChangeEvent>
        {
            public bool Equals(FileChangeEvent x, FileChangeEvent y) => default(AbstractPath.DefaultEqualityComparer).Equals(x.AbstractPath, y.AbstractPath);
            public int GetHashCode(FileChangeEvent obj) => default(AbstractPath.DefaultEqualityComparer).GetHashCode(obj.AbstractPath);
        }
    }
}
