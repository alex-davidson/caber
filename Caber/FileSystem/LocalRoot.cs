using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Caber.FileSystem
{
    /// <summary>
    /// Represents a directory on the OS filesystem.
    /// </summary>
    /// <remarks>
    /// Each of these should be created and validated once at application startup and
    /// reused thereafter. Reference equality should be used.
    /// Beware that the Uri class treats local and UNC paths case-insensitively! This
    /// should not be a problem unless multiple roots are used which differ only by case.
    /// </remarks>
    public sealed class LocalRoot
    {
        internal Uri RootUri { get; }
        public FileSystemCasing Casing { get; }

        public LocalRoot(Uri rootUri, FileSystemCasing casing)
        {
            if (rootUri == null) throw new ArgumentNullException(nameof(rootUri));
            if (!rootUri.IsAbsoluteUri) throw new ArgumentException($"Not an absolute path: {rootUri.LocalPath}", nameof(rootUri));
            if (!PathUtils.IsDirectoryUri(rootUri)) throw new ArgumentException($"Not a directory path: {rootUri.LocalPath}", nameof(rootUri));
            if (!Enum.IsDefined(typeof(FileSystemCasing), casing))
            {
                throw new InvalidEnumArgumentException(nameof(casing), (int)casing, typeof(FileSystemCasing));
            }
            if (casing == FileSystemCasing.Unspecified) throw new ArgumentException($"Must specify casing rule for path {rootUri}", nameof(casing));
            RootUri = rootUri;
            Casing = casing;
        }

        public override string ToString() => RootUri.LocalPath;

        /// <summary>
        /// Use this with care. The Uri class treats local and UNC paths case-insensitively.
        /// </summary>
        public struct RootUriEqualityComparer : IEqualityComparer<LocalRoot>
        {
            public bool Equals(LocalRoot x, LocalRoot y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.RootUri.Equals(y.RootUri);
            }

            public int GetHashCode(LocalRoot obj)
            {
                return obj.RootUri.GetHashCode();
            }
        }
    }
}
