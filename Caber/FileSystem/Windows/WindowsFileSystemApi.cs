using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Caber.FileSystem.Windows
{
    public class WindowsFileSystemApi : IFileSystemApi
    {
        public RelativePath GetCanonicalRelativePath(string rootPath, IEnumerable<string> relativePathParts)
        {
            var parts = relativePathParts.ToArray();
            new WindowsPathCanonicaliser().TryCanonicaliseRelative(new Internal(), rootPath, parts);
            return RelativePath.CreateFromSegments(parts);
        }

        public LocalRoot CreateStorageRoot(string absoluteFileSystemPath, FileSystemCasing casing)
        {
            if (!PathUtils.IsNormalisedAbsolutePath(absoluteFileSystemPath)) throw new ArgumentException($"Not a valid absolute path: {absoluteFileSystemPath}", nameof(absoluteFileSystemPath));

            var canonicalUri = new WindowsPathCanonicaliser().CanonicaliseAbsolute(new Internal(), absoluteFileSystemPath);

            if (casing == FileSystemCasing.Unspecified) return new LocalRoot(canonicalUri, GetActualCasingRules(canonicalUri));
            return new LocalRoot(canonicalUri, casing);
        }

        private static FileSystemCasing GetActualCasingRules(Uri fileSystemUri)
        {
            // .NET provides no sane means of determining this right now. The following insane
            // means are known, but unacceptable or too much effort right now:
            // * Create a file with one casing and see if it exists with other casing (unacceptable),
            // * P/Invoke (too much effort),
            // * Invoke a console tool to read dir attributes (too much effort).
            return FileSystemCasing.CasePreservingInsensitive;
        }

        public class Internal
        {
            public virtual FileSystemInfo Lookup(string path)
            {
                if (File.Exists(path)) return new FileInfo(path);
                if (Directory.Exists(path)) return new DirectoryInfo(path);
                return null;
            }

            public virtual FileSystemInfo Lookup(DirectoryInfo container, string item)
            {
                if (!container.Exists) return null;
                return container.GetFileSystemInfos(item).SingleOrDefault();
            }
        }
    }
}
