using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Caber.FileSystem
{
    internal static class PathUtils
    {
        public static bool IsNormalisedAbsolutePath(string path)
        {
            if (!Path.IsPathRooted(path)) return false;
            if (Path.GetFullPath(path) != path) return false;
            return true;
        }

        public static bool TryGetRelativeSegments(Uri rootUri, Uri absoluteUri, out string[] relativePathParts)
        {
            relativePathParts = null;
            if (!rootUri.IsBaseOf(absoluteUri)) return false;
            Debug.Assert(string.IsNullOrWhiteSpace(absoluteUri.Query));
            var relativePath = rootUri.MakeRelativeUri(absoluteUri).ToString();
            relativePathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return true;
        }

        public static string[] GetRelativePathSegments(string relativePath)
        {
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException($"Not a relative path: {relativePath}", nameof(relativePath));
            return relativePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static bool IsDirectoryUri(Uri absoluteUri)
        {
            if (!string.IsNullOrEmpty(absoluteUri.Query)) throw new InvalidOperationException($"Query string is not supported for filesystem URIs: {absoluteUri.Query}");
            return absoluteUri.PathAndQuery.EndsWith("/");
        }

        public static string AsDirectoryPath(this string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }
    }
}
