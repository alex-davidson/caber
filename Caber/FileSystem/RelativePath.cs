using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Caber.FileSystem
{
    /// <summary>
    /// Encapsulates a normalised, correctly-cased file path relative to some root, using
    /// '/' as the directory separator.
    /// </summary>
    public sealed class RelativePath
    {
        private const char SeparatorChar = '/';
        private const string Separator = "/";
        private readonly string path;

        private RelativePath(string path)
        {
            this.path = path;
        }

        public override string ToString() => path;
        public bool IsContainer() => path.EndsWith(Separator);

        public RelativePath RemovePrefix(RelativePath prefix, PathEqualityComparer comparer)
        {
            if (!prefix.IsContainer()) throw new ArgumentException($"Cannot treat non-container path as a prefix: {prefix}", nameof(prefix));
            if (!prefix.Contains(this, comparer)) throw new ArgumentException($"Path '{prefix}' is not a prefix of '{this}'", nameof(prefix));
            return new RelativePath(path.Substring(prefix.path.Length));
        }

        public bool Contains(RelativePath other, PathEqualityComparer comparer)
        {
            if (!IsContainer()) return false;
            if (other.path.Length <= path.Length) return false;
            return comparer.Equals(other.path.Substring(0, path.Length), path);
        }

        public static RelativePath Combine(params RelativePath[] paths) => CombineImpl(paths);
        public static RelativePath Combine(IEnumerable<RelativePath> paths) => CombineImpl(paths);
        public static RelativePath CombineImpl(IEnumerable<RelativePath> paths)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            using (var iterator = paths.GetEnumerator())
            {
                if (!iterator.MoveNext()) throw new ArgumentException("RelativePath must have at least one segment.", nameof(paths));

                var sb = new StringBuilder();
                var lastWasContainer = iterator.Current.IsContainer();
                sb.Append(iterator.Current);
                while (iterator.MoveNext())
                {
                    if (!lastWasContainer)
                    {
                        throw new ArgumentException($"Non-container path must be the last one passed to {typeof(RelativePath)}::{nameof(Combine)}: {iterator.Current}", nameof(paths));
                    }
                    lastWasContainer = iterator.Current.IsContainer();
                    sb.Append(iterator.Current);
                }
                return new RelativePath(sb.ToString());
            }
        }

        public static RelativePath CreateFromSegments(params string[] segments)
        {
            if (!segments.Any()) throw new ArgumentException("RelativePath must have at least one segment.", nameof(segments));
            var invalid = segments.FirstOrDefault(p => p.Contains(SeparatorChar));
            if (invalid != null) throw new ArgumentException($"RelativePath segment may not contain separator: {invalid}");
            return new RelativePath(string.Join(Separator, segments));
        }

        public RelativePath AsContainer()
        {
            if (IsContainer()) return this;
            return new RelativePath(path + SeparatorChar);
        }
    }
}
