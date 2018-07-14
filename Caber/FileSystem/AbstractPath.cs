using System.Collections.Generic;

namespace Caber.FileSystem
{
    public sealed class AbstractPath
    {
        public NamedRoot Root { get; }
        public RelativePath RelativePath { get; }

        public AbstractPath(NamedRoot root, RelativePath relativePath)
        {
            Root = root;
            RelativePath = relativePath;
        }

        public override string ToString() => $"<{Root.Name}>:{RelativePath}";

        public struct DefaultEqualityComparer : IEqualityComparer<AbstractPath>
        {
            public bool Equals(AbstractPath x, AbstractPath y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (!Equals(x.Root, y.Root)) return false;
                var comparer = new PathEqualityComparer(x.Root.LocalRoot.Casing);
                return comparer.Equals(x.RelativePath, y.RelativePath);
            }

            public int GetHashCode(AbstractPath obj)
            {
                unchecked
                {
                    var comparer = new PathEqualityComparer(obj.Root.LocalRoot.Casing);
                    return (obj.Root.GetHashCode() * 397) ^ comparer.GetHashCode(obj.RelativePath);
                }
            }
        }
    }
}
