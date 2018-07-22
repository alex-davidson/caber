using System;
using System.Diagnostics;
using System.IO;

namespace Caber.FileSystem.Windows
{
    internal class WindowsPathCanonicaliser
    {
        /// <summary>
        /// Adjust the casing of each element of 'parts' to match that on the filesystem.
        /// </summary>
        /// <remarks>
        /// Does no input validation.
        /// The 'parts' array is modified in-place.
        /// </remarks>
        public bool TryCanonicaliseRelative(WindowsFileSystemApi.Internal api, string containerPath, string[] parts)
        {
            var container = api.Lookup(containerPath) as DirectoryInfo;
            for (var i = 0; i < parts.Length; i++)
            {
                if (container == null) return false;
                var current = api.Lookup(container, parts[i]);
                if (current == null) return false;
                parts[i] = current.Name;
                container = current as DirectoryInfo;
            }
            return true;
        }

        /// <summary>
        /// Create a canonical Uri from 'absoluteFileSystemPath' by adjusting the casing of each
        /// non-root segment.
        /// </summary>
        /// <remarks>
        /// Does no input validation.
        /// </remarks>
        public Uri CanonicaliseAbsolute(WindowsFileSystemApi.Internal api, string absoluteFileSystemPath)
        {
            var absoluteUri = new Uri(absoluteFileSystemPath);
            var root = Path.GetPathRoot(absoluteFileSystemPath);
            Debug.Assert(absoluteFileSystemPath.StartsWith(root));

            var rootUri = new Uri(root, UriKind.Absolute);
            if (!PathUtils.TryGetRelativeSegments(rootUri, absoluteUri, out var parts))
            {
                throw new ArgumentException($"Unable to parse path: {absoluteFileSystemPath}", nameof(absoluteFileSystemPath));
            }
            TryCanonicaliseRelative(api, root, parts);
            return new Uri(rootUri, Path.Combine(parts));
        }
    }
}
