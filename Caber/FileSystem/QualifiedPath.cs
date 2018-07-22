using System;
using System.Collections.Generic;

namespace Caber.FileSystem
{
    public sealed class QualifiedPath
    {
        public QualifiedPath(LocalRoot root, RelativePath relativePath)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        }

        public LocalRoot Root { get; }
        public RelativePath RelativePath { get; }

        public override string ToString() => $"{Root}:{RelativePath}";

        public struct DefaultEqualityComparer : IEqualityComparer<QualifiedPath>
        {
            public bool Equals(QualifiedPath x, QualifiedPath y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (!ReferenceEquals(x.Root, y.Root)) return false;
                var comparer = new PathEqualityComparer(x.Root.Casing);
                return comparer.Equals(x.RelativePath, y.RelativePath);
            }

            public int GetHashCode(QualifiedPath obj)
            {
                unchecked
                {
                    var comparer = new PathEqualityComparer(obj.Root.Casing);
                    return (obj.Root.GetHashCode() * 397) ^ comparer.GetHashCode(obj.RelativePath);
                }
            }
        }
    }
}
