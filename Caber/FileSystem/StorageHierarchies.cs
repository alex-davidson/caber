using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Caber.FileSystem.Filters;
using Caber.Logging;

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
        private readonly IDictionary<LocalRoot, RelativePathFilter> filtersByRoot;
        private readonly List<LocalRoot> rootsLongestToShortest;
        private readonly IDictionary<LocalRoot, Graft> graftsByChild;
        private readonly IDictionary<LocalRoot, Graft[]> graftsByParent;

        public StorageHierarchies(IFileSystemApi fileSystemApi, NamedRoot[] namedRoots, Graft[] grafts, IDictionary<LocalRoot, RelativePathFilter> filtersByRoot)
        {
            this.fileSystemApi = fileSystemApi;
            this.namedRoots = namedRoots;
            this.filtersByRoot = filtersByRoot;

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

        public RelativePathFilter GetFilterFor(LocalRoot root)
        {
            AssertLocalRootExistsInModel(root);
            filtersByRoot.TryGetValue(root, out var filter);
            return filter;
        }

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

        public AbstractPath MapToAbstractPath(QualifiedPath qualifiedPath, IDiagnosticsLog log = null)
        {
            var currentNode = qualifiedPath.Root;
            AssertLocalRootExistsInModel(currentNode);
            var traversed = new HashSet<LocalRoot> { currentNode };
            var paths = new Stack<RelativePath>();
            paths.Push(qualifiedPath.RelativePath);
            if (!PassesFilter(currentNode, paths, log)) return null;
            while (graftsByChild.TryGetValue(currentNode, out var graft))
            {
                currentNode = graft.GraftPoint.Root;
                paths.Push(graft.GraftPoint.RelativePath);
                // Sanity check. Should be impossible to configure such a situation.
                if (!traversed.Add(currentNode)) throw new InvalidOperationException("Cycle detected in graph.");
                if (!PassesFilter(currentNode, paths, log)) return null;
            }
            foreach (var namedRoot in namedRoots)
            {
                if (namedRoot.LocalRoot != currentNode) continue;
                return new AbstractPath(namedRoot, RelativePath.Combine(paths));
            }
            return null;
        }

        private bool PassesFilter(LocalRoot root, IEnumerable<RelativePath> paths, IDiagnosticsLog log = null)
        {
            var filter = GetFilterFor(root);
            if (!filter.Exists) return true;
            var relativePath = RelativePath.Combine(paths);
            var match = filter.Evaluate(relativePath);
            switch (match.Rule)
            {
                case FilterRule.Exclude:
                    log?.Debug(new ExcludedByFilterEvent(root, relativePath, match));
                    return false;
                case FilterRule.Include:
                    if (default(RelativePathMatcher).Equals(match))
                    {
                        log?.Debug(new ExcludedByFilterEvent(root, relativePath, match));
                    }
                    return true;
                default:
                    throw new ArgumentOutOfRangeException($"Not a valid rule type: {match.Rule}");
            }
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

        public abstract class FilterEvent : LogEvent, ILogEventJsonDto
        {
            private readonly LocalRoot localRoot;
            private readonly RelativePath relativePath;

            protected FilterEvent(LocalRoot localRoot, RelativePath relativePath)
            {
                this.localRoot = localRoot;
                this.relativePath = relativePath;
            }

            public override LogEventCategory Category => LogEventCategory.Storage;
            public override ILogEventJsonDto GetDtoForJson() => this;

            public string LocalRoot => localRoot.ToString();
            public string Path => relativePath.ToString();
        }

        public class IncludedByFilterEvent : FilterEvent
        {
            private readonly RelativePathMatcher matcher;

            public IncludedByFilterEvent(LocalRoot localRoot, RelativePath relativePath, RelativePathMatcher matcher) : base(localRoot, relativePath)
            {
                if (matcher.Rule != FilterRule.Include) throw new ArgumentException("Matcher is not an include rule.");
                this.matcher = matcher;
            }

            public override string FormatMessage() => $"Include rule matched: {Matcher} ~= {LocalRoot}:{Path}";

            public string Matcher => matcher.ToString();
        }

        public class ExcludedByFilterEvent : FilterEvent
        {
            private readonly RelativePathMatcher matcher;

            public ExcludedByFilterEvent(LocalRoot localRoot, RelativePath relativePath, RelativePathMatcher matcher) : base(localRoot, relativePath)
            {
                if (matcher.Rule != FilterRule.Exclude) throw new ArgumentException("Matcher is not an exclude rule.");
                this.matcher = matcher;
            }

            public override string FormatMessage() => $"Exclude rule matched: {Matcher} ~= {LocalRoot}:{Path}";

            public string Matcher => matcher.ToString();
        }
    }
}
