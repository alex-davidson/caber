using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caber.Configuration;
using Caber.FileSystem.Filters;
using Caber.FileSystem.Validation;
using Caber.Util;

namespace Caber.FileSystem
{
    public class StorageHierarchiesBuilder
    {
        private readonly IFileSystemApi fileSystemApi;
        private readonly HashSet<LocalRoot> nodes = new HashSet<LocalRoot>();
        private readonly HashSet<LocalRoot> nodePaths = new HashSet<LocalRoot>(default(LocalRoot.RootUriEqualityComparer));
        private readonly Dictionary<string, NamedRoot> namedRoots = new Dictionary<string, NamedRoot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<QualifiedPath, Graft> grafts = new Dictionary<QualifiedPath, Graft>(default(QualifiedPath.DefaultEqualityComparer));
        private readonly Dictionary<LocalRoot, List<RelativePathMatcher>> filters = new Dictionary<LocalRoot, List<RelativePathMatcher>>();

        private readonly ErrorCollection errors = new ErrorCollection();
        /// <summary>
        /// Errors encountered while building the hierarchy.
        /// </summary>
        /// <remarks>
        /// This does not include 'simple' errors thrown as exceptions. Anything which can be checked
        /// in isolation (eg. absolute paths) should be handled by the caller.
        /// These errors are detected only in the context of the hierarchy as a whole.
        /// </remarks>
        public IErrorCollection Errors => errors;

        public StorageHierarchiesBuilder(IFileSystemApi fileSystemApi)
        {
            this.fileSystemApi = fileSystemApi;
        }

        public LocalRoot CreateNode(string absolutePath, FileSystemCasing casing)
        {
            if (!Path.IsPathRooted(absolutePath)) throw new ArgumentException($"Not an absolute path: {absolutePath}", nameof(absolutePath));
            return fileSystemApi.CreateStorageRoot(absolutePath.AsDirectoryPath(), casing);
        }

        public ConfigurationRuleViolation AddNamedRoot(string name, LocalRoot node)
        {
            var namedRoot = new NamedRoot(name, node);
            if (namedRoots.TryGetValue(namedRoot.Name, out var conflict)) return errors.Record(new DuplicateNamedRootDeclaration(namedRoot, conflict));
            if (!TryAddLocalRoot(node, out var violation)) return errors.Record(violation);
            namedRoots.Add(namedRoot.Name, namedRoot);
            return null;
        }

        public ConfigurationRuleViolation AddGraftPoint(LocalRoot parent, string relativePath, LocalRoot child)
        {
            if (!nodes.Contains(parent)) throw new InvalidOperationException($"Parent is not yet declared: {parent}");
            var graft = CreateGraftInternal(parent, relativePath, child);
            return AddGraftInternal(graft);
        }

        private Graft CreateGraftInternal(LocalRoot parent, string relativePath, LocalRoot child)
        {
            var segments = PathUtils.GetRelativePathSegments(relativePath);
            var graftPath = fileSystemApi.GetCanonicalRelativePath(parent.RootUri.LocalPath, segments).AsContainer();
            return new Graft(new QualifiedPath(parent, graftPath), child);
        }

        private ConfigurationRuleViolation AddGraftInternal(Graft graft)
        {
            if (grafts.TryGetValue(graft.GraftPoint, out var conflict)) return errors.Record(new DuplicateGraftDeclaration(graft, conflict));
            if (graft.ChildRoot.Casing != graft.GraftPoint.Root.Casing) return errors.Record(new FileSystemCasingConflict(graft));
            if (!TryAddLocalRoot(graft.ChildRoot, out var violation)) return errors.Record(violation);
            grafts.Add(graft.GraftPoint, graft);
            return null;
        }

        public ConfigurationRuleViolation AddFilter(LocalRoot owner, RelativePathMatcher filterRule)
        {
            if (!nodes.Contains(owner)) throw new InvalidOperationException($"Owner is not yet declared: {owner}");

            if (!filters.TryGetValue(owner, out var list))
            {
                list = new List<RelativePathMatcher>();
                filters.Add(owner, list);
            }
            list.Add(filterRule);
            return null;
        }

        public StorageHierarchies BuildHierarchies()
        {
            return new StorageHierarchies(
                fileSystemApi,
                namedRoots.Values.ToArray(),
                grafts.Values.ToArray(),
                filters.ToDictionary(f => f.Key, f => new RelativePathFilter(f.Value)));
        }

        private bool TryAddLocalRoot(LocalRoot node, out ConfigurationRuleViolation violation)
        {
            if (nodes.Contains(node))
            {
                violation = new DuplicateLocalRootDeclaration(node);
                return false;
            }
            if (nodePaths.Contains(node))
            {
                violation = new DuplicateLocalRootDeclaration(node);
                return false;
            }
            foreach (var existing in nodes)
            {
                if (node.RootUri.IsBaseOf(existing.RootUri))
                {
                    violation = new OverlappingLocalRootDeclaration(node, existing);
                    return false;
                }
                if (existing.RootUri.IsBaseOf(node.RootUri))
                {
                    violation = new OverlappingLocalRootDeclaration(existing, node);
                    return false;
                }
            }
            nodes.Add(node);
            nodePaths.Add(node);
            violation = null;
            return true;
        }
    }
}
