using System;
using System.Diagnostics;
using System.IO;
using Caber.Configuration.Storage.Validation;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using Caber.Util;

namespace Caber.Configuration.Storage
{
    public class StorageConfigurationReader
    {
        private readonly ErrorCollection errors = new ErrorCollection();
        /// <summary>
        /// Errors encountered while reading the configuration elements.
        /// </summary>
        /// <remarks>
        /// Anything which can be checked in isolation (eg. absolute paths, attribute formats) should appear
        /// here. Errors detectable only with the context of the hierarchy will be recorded by the builder.
        /// </remarks>
        public IErrorCollection Errors => errors;

        public void Read(StorageElementCollection element, StorageHierarchiesBuilder builder)
        {
            foreach (var add in element.Items) Read(add, builder);
        }

        public void Read(StorageElementCollection.AddElement add, StorageHierarchiesBuilder builder)
        {
            var context = TryCreateNamedHierarchyAndContext(add, builder);
            ReadFiltersElement(add.Filters, context);
            ReadChildLocations(add, context);
        }

        private void ReadChildLocations(LocationElementCollection locations, Context parentContext)
        {
            foreach (var child in locations.Children)
            {
                if (child.Graft == null)
                {
                    ReadLocationElement(child, parentContext);
                }
                else
                {
                    ReadGraftPointElement(child, parentContext);
                }
            }
        }

        private void ReadLocationElement(LocationElement location, Context parentContext)
        {
            var context = TryCreateLocationAndContext(location, parentContext);
            ReadFiltersElement(location.Filters, context);
            ReadChildLocations(location, context);
        }

        private void ReadGraftPointElement(LocationElement location, Context parentContext)
        {
            var context = TryCreateGraftPointAndContext(location, parentContext);
            ReadFiltersElement(location.Filters, context);
            ReadChildLocations(location, context);
        }

        private Context TryCreateNamedHierarchyAndContext(StorageElementCollection.AddElement add, StorageHierarchiesBuilder builder)
        {
            var watch = builder.Errors.Watch() + Errors.Watch();
            VerifyAbsolutePath(add.Path)?.SetLocation(add.ElementInformation);
            if (watch.HasNewErrors) return default;

            var root = builder.CreateNode(add.Path, FileSystemCasing.Unspecified);
            builder.AddNamedRoot(add.Name, root)?.SetLocation(add.ElementInformation);
            if (watch.HasNewErrors) return Context.CreateForValidation(root);

            builder.AddFilter(root, CreateExcludeReservedPathsFilter(root));
            return Context.Create(builder, root);
        }

        private Context TryCreateLocationAndContext(LocationElement location, Context context)
        {
            Debug.Assert(location.Graft == null);
            var watch = context.Watch() + Errors.Watch();
            VerifyRelativePath(location.Path)?.SetLocation(location.ElementInformation);
            if (watch.HasNewErrors || context.IsValidationOnly) return default;

            context.AddLocation(location.Path, out var graftedRoot)?.SetLocation(location.ElementInformation);
            if (watch.HasNewErrors) return Context.CreateForValidation(graftedRoot);

            return context.CreateChildContext(graftedRoot);
        }

        private Context TryCreateGraftPointAndContext(LocationElement location, Context context)
        {
            Debug.Assert(location.Graft != null);
            var watch = context.Watch() + Errors.Watch();
            VerifyRelativePath(location.Path)?.SetLocation(location.ElementInformation);
            VerifyAbsolutePath(location.Graft)?.SetLocation(location.ElementInformation);
            if (watch.HasNewErrors || context.IsValidationOnly) return default;

            var graftedRoot = context.CreateNode(location.Graft, FileSystemCasing.Unspecified);
            context.AddGraftPoint(location.Path, graftedRoot)?.SetLocation(location.ElementInformation);
            if (watch.HasNewErrors) return Context.CreateForValidation(graftedRoot);

            return context.CreateChildContext(graftedRoot);
        }

        private void ReadFiltersElement(FilterCollection filters, Context context)
        {
            if (filters == null) return;
            var reader = new FilterConfigurationReader(errors, context.Casing);
            foreach (var filter in filters.Filters)
            {
                var matcher = reader.Read(filter);
                context.AddFilter(matcher); // ?.SetLocation(filter.ElementInformation);
            }
        }

        private ConfigurationRuleViolation VerifyAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path)) return null;
            return errors.Record(new AbsolutePathRequired(path));
        }

        private ConfigurationRuleViolation VerifyRelativePath(string path)
        {
            if (!Path.IsPathRooted(path)) return null;
            return errors.Record(new RelativePathRequired(path));
        }

        private RelativePathMatcher CreateExcludeReservedPathsFilter(LocalRoot root)
        {
            var regex = new GlobToRegexCompiler().CompileRegex("**/.caber/**", root.Casing);
            return new RelativePathMatcher(regex, FilterRule.Exclude, "Glob(**/.caber/**)");
        }

        /// <summary>
        /// Encapsulates the StorageHierarchiesBuilder and our current LocalRoot as we explore the tree.
        /// </summary>
        /// <remarks>
        /// Operations which only apply at the current level of the tree should require neither a builder
        /// nor a current node. This permits validation to continue even if construction of a parent node
        /// fails.
        /// </remarks>
        private struct Context
        {
            private readonly StorageHierarchiesBuilder builder;
            private readonly LocalRoot currentContainer;

            private Context(StorageHierarchiesBuilder builder, LocalRoot currentContainer)
            {
                this.builder = builder;
                this.currentContainer = currentContainer;
            }

            public bool IsValidationOnly => builder == null;

            /* Validation-only contexts may attempt operations which apply to the current node, even when there is no current node. */

            public FileSystemCasing Casing => currentContainer?.Casing ?? FileSystemCasing.Unspecified;
            public ErrorWatcher Watch() => builder?.Errors.Watch() ?? default;
            public ConfigurationRuleViolation AddFilter(RelativePathMatcher filterRule) => builder?.AddFilter(currentContainer, filterRule);

            /* Node creation and recursion are not valid when the context IsValidationOnly. */

            public string ResolveImpliedGraftPoint(string relativePath) => new Uri(currentContainer.RootUri, relativePath).LocalPath;
            public LocalRoot CreateNode(string absolutePath, FileSystemCasing casing) => builder.CreateNode(absolutePath, casing);
            public ConfigurationRuleViolation AddLocation(string relativePath, out LocalRoot child) => builder.AddLocation(currentContainer, relativePath, out child);
            public ConfigurationRuleViolation AddGraftPoint(string relativePath, LocalRoot child) => builder.AddGraftPoint(currentContainer, relativePath, child);
            public Context CreateChildContext(LocalRoot child) => builder == null ? throw new InvalidOperationException() : new Context(builder, child);

            public static Context CreateForValidation(LocalRoot node) => new Context(null, node);
            public static Context Create(StorageHierarchiesBuilder builder, LocalRoot node) => new Context(builder, node);
        }
    }
}
