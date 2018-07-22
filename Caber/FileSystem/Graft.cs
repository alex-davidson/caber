using System;
using System.Collections.Generic;

namespace Caber.FileSystem
{
    public sealed class Graft
    {
        public QualifiedPath GraftPoint { get; }
        public LocalRoot ChildRoot { get; }

        public Graft(QualifiedPath graftPoint, LocalRoot childRoot)
        {
            if (!graftPoint.RelativePath.IsContainer()) throw new ArgumentException($"Graft point must be a container: {graftPoint}", nameof(graftPoint));
            GraftPoint = graftPoint;
            ChildRoot = childRoot ?? throw new ArgumentNullException(nameof(childRoot));
        }

        public override string ToString() => $"{GraftPoint} -> {ChildRoot}";

        public struct DefaultEqualityComparer : IEqualityComparer<Graft>
        {
            public bool Equals(Graft x, Graft y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                if (!x.ChildRoot.Equals(y.ChildRoot)) return false;
                return default(QualifiedPath.DefaultEqualityComparer).Equals(x.GraftPoint, y.GraftPoint);
            }

            public int GetHashCode(Graft obj)
            {
                unchecked
                {
                    return ( default(QualifiedPath.DefaultEqualityComparer).GetHashCode(obj.GraftPoint) * 397) ^ obj.ChildRoot.GetHashCode();
                }
            }
        }
    }
}
