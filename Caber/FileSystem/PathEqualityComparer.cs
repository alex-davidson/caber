using System;
using System.Collections.Generic;

namespace Caber.FileSystem
{
    public struct PathEqualityComparer :
        IEqualityComparer<string>,
        IEqualityComparer<RelativePath>
    {
        private readonly StringComparer comparer;

        private static StringComparer SelectComparer(FileSystemCasing casing)
        {
            switch (casing)
            {
                case FileSystemCasing.Unspecified: throw new ArgumentException("Cannot compare paths when the casing rules are not known.");
                case FileSystemCasing.CaseSensitive: return StringComparer.Ordinal;
                case FileSystemCasing.CasePreservingInsensitive: return StringComparer.OrdinalIgnoreCase;
                default: throw new ArgumentOutOfRangeException(nameof(casing), casing, null);
            }
        }

        public PathEqualityComparer(FileSystemCasing casing)
        {
            comparer = SelectComparer(casing);
        }

        public bool Equals(string x, string y) => comparer.Equals(x, y);
        public int GetHashCode(string obj) => comparer.GetHashCode(obj);

        public bool Equals(RelativePath x, RelativePath y) => comparer.Equals(x?.ToString(), y?.ToString());
        public int GetHashCode(RelativePath obj) => comparer.GetHashCode(obj.ToString());
    }
}
