﻿using System;
using System.Collections.Generic;
using System.Linq;
using Caber.FileSystem;

namespace Caber.UnitTests.FileSystem
{
    internal class StubFileSystemApi : IFileSystemApi
    {
        public RelativePath GetCanonicalRelativePath(string rootPath, IEnumerable<string> relativePathParts)
        {
            return RelativePath.CreateFromSegments(relativePathParts.ToArray());
        }

        public LocalRoot CreateStorageRoot(string absoluteFileSystemPath, FileSystemCasing casing)
        {
            if (!PathUtils.IsNormalisedAbsolutePath(absoluteFileSystemPath)) throw new ArgumentException($"Not a valid absolute path: {absoluteFileSystemPath}", nameof(absoluteFileSystemPath));
            var canonicalUri = new Uri(absoluteFileSystemPath);
            return new LocalRoot(canonicalUri, casing);
        }
    }
}
