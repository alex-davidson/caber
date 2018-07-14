using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Caber.FileSystem
{
    /// <summary>
    /// Maps between real OS paths and abstract hierarchies.
    /// </summary>
    /// <remarks>
    /// This class is considerably more powerful and flexible than is actually required
    /// at present, supporting such things as overlapping hierarchies. The reasons for
    /// this is that eliminating undefined behaviour was simple, doing so flushed out
    /// some tricky edge cases, and should that behaviour ever be hit by accident it
    /// should at least behave in a sane and consistent manner.
    /// </remarks>
    public class StorageHierarchies
    {
        private readonly IFileSystemApi fileSystemApi;
        private readonly NamedRoot[] namedRoots;
        private readonly List<LocalRoot> rootsLongestToShortest;
        private readonly IDictionary<LocalRoot, Graft> graftsByChild;
        private readonly IDictionary<LocalRoot, Graft[]> graftsByParent;

        public StorageHierarchies(IFileSystemApi fileSystemApi, NamedRoot[] namedRoots, Graft[] grafts)
        {
            this.fileSystemApi = fileSystemApi;
            this.namedRoots = namedRoots;

            graftsByChild = grafts.ToDictionary(g => g.ChildRoot);
            graftsByParent = grafts.GroupBy(g => g.GraftPoint.Root).ToDictionary(g => g.Key, g => g.ToArray());
            var allRoots = namedRoots.Select(r => r.LocalRoot).Concat(graftsByChild.Keys);
            rootsLongestToShortest = allRoots.Distinct().OrderByDescending(r => r.RootUri.LocalPath.Length).ToList();

            AllRoots = new ReadOnlyCollection<LocalRoot>(rootsLongestToShortest);
            NamedRoots = new ReadOnlyCollection<NamedRoot>(namedRoots);
            Grafts = new ReadOnlyCollection<Graft>(grafts);
        }

        public ICollection<LocalRoot> AllRoots { get; }
        public ICollection<NamedRoot> NamedRoots { get; }
        public ICollection<Graft> Grafts { get; }

        /// <summary>
        /// Express a local path as a known root and a relative canonicalised path.
        /// </summary>
        /// <remarks>
        /// Finds the storage root closest to the path, ie. smallest enclosing subtree.
        /// Returns false if no root can be found, or if the qualified path exists under a graft
        /// point and is therefore 'shadowed'.
        /// </remarks>
        public bool TryResolveQualifiedPath(string absolutePath, out QualifiedPath qualifiedPath)
        {
            qualifiedPath = default;
            var uri = new Uri(absolutePath);
            foreach (var root in rootsLongestToShortest)
            {
                if (!PathUtils.TryGetRelativeSegments(root.RootUri, uri, out var relativePathParts)) continue;
                var canonicalisedPath = fileSystemApi.GetCanonicalRelativePath(root.RootUri.LocalPath, relativePathParts);

                // Found a 'best' qualified path.
                qualifiedPath = new QualifiedPath(root, canonicalisedPath);

                if (!graftsByParent.TryGetValue(root, out var grafts)) return true; // No grafts present to 'shadow' this hierarchy.
                var comparer = new PathEqualityComparer(root.Casing);
                if (grafts.Any(g => g.GraftPoint.RelativePath.Contains(canonicalisedPath, comparer)))
                {
                    // The qualified path is shadowed by a graft point.
                    return false;
                }
                return true;
            }
            // No containing root was found.
            return false;
        }

        /// <summary>
        /// Resolve the qualified path to an OS path and return a FileInfo wrapper.
        /// </summary>
        public FileInfo ResolveToFile(QualifiedPath qualifiedPath)
        {
            AssertLocalRootExistsInModel(qualifiedPath.Root);
            if (qualifiedPath.RelativePath.IsContainer()) throw new ArgumentException($"Cannot resolve a container path to a file: {qualifiedPath}", nameof(qualifiedPath));
            var absoluteUri = new Uri(qualifiedPath.Root.RootUri, qualifiedPath.RelativePath.ToString());
            return new FileInfo(absoluteUri.LocalPath);
        }

        /// <summary>
        /// Find the named root which ultimately owns the path.
        /// </summary>
        public NamedRoot FindNamedRoot(QualifiedPath path)
        {
            var currentNode = path.Root;
            AssertLocalRootExistsInModel(currentNode);
            var traversed = new HashSet<LocalRoot> { currentNode };
            while (graftsByChild.TryGetValue(currentNode, out var graft))
            {
                currentNode = graft.ChildRoot;
                // Sanity check. Should be impossible to configure such a situation.
                if (!traversed.Add(currentNode)) throw new InvalidOperationException("Cycle detected in graph.");
            }
            return namedRoots.FirstOrDefault(r => r.LocalRoot == currentNode);
        }

        public AbstractPath MapToAbstractPath(QualifiedPath qualifiedPath)
        {
            var currentNode = qualifiedPath.Root;
            AssertLocalRootExistsInModel(currentNode);
            var traversed = new HashSet<LocalRoot> { currentNode };
            var paths = new Stack<RelativePath>();
            paths.Push(qualifiedPath.RelativePath);
            while (graftsByChild.TryGetValue(currentNode, out var graft))
            {
                currentNode = graft.GraftPoint.Root;
                paths.Push(graft.GraftPoint.RelativePath);
                // Sanity check. Should be impossible to configure such a situation.
                if (!traversed.Add(currentNode)) throw new InvalidOperationException("Cycle detected in graph.");
            }
            foreach (var namedRoot in namedRoots)
            {
                if (namedRoot.LocalRoot != currentNode) continue;
                return new AbstractPath(namedRoot, RelativePath.Combine(paths));
            }
            return null;
        }

        public QualifiedPath MapFromAbstractPath(AbstractPath abstractPath)
        {
            var comparer = new PathEqualityComparer(abstractPath.Root.LocalRoot.Casing);
            var currentNode = abstractPath.Root.LocalRoot;
            AssertLocalRootExistsInModel(currentNode);
            var currentRelativePath = abstractPath.RelativePath;

            while (graftsByParent.TryGetValue(currentNode, out var grafts))
            {
                var container = grafts.FirstOrDefault(g => g.GraftPoint.RelativePath.Contains(currentRelativePath, comparer));
                if (container == null) break;
                currentRelativePath = currentRelativePath.RemovePrefix(container.GraftPoint.RelativePath, comparer);
                currentNode = container.ChildRoot;
            }
            return new QualifiedPath(currentNode, currentRelativePath);
        }

        private void AssertLocalRootExistsInModel(LocalRoot root)
        {
            if (rootsLongestToShortest.Contains(root)) return;
            throw new InvalidOperationException($"Unrecognised LocalRoot: {root}");
        }
    }
}
