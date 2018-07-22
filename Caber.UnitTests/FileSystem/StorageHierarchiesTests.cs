using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    [TestFixture]
    public class StorageHierarchiesTests
    {
        [Test]
        public void RoundtripsQualifiedPathInRootWithNoGrafts()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("SimpleRoot", root);
            var storage = builder.BuildHierarchies();

            Assert.That(storage.TryResolveQualifiedPath(@"C:\root\some\filepath.txt", out var qualifiedPath), Is.True);
            Assert.That(qualifiedPath,
                Is.EqualTo(new QualifiedPath(root, RelativePath.CreateFromSegments("some", "filepath.txt")))
                    .Using(default(QualifiedPath.DefaultEqualityComparer)));

            Assert.That(storage.ResolveToFile(qualifiedPath)?.FullName, Is.EqualTo(@"C:\Root\some\filepath.txt"));
        }

        [Test]
        public void DoesNotResolveQualifiedPathNotInRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("SimpleRoot", root);
            var storage = builder.BuildHierarchies();

            Assert.That(storage.TryResolveQualifiedPath(@"C:\Outside\some\filepath.txt", out _), Is.False);
        }

        [Test]
        public void DoesNotResolveQualifiedPathInRoot_UnderGraftPoint()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);
            builder.AddGraftPoint(root, "Graft", builder.CreateNode(@"C:\Other", FileSystemCasing.CasePreservingInsensitive));
            var storage = builder.BuildHierarchies();

            Assert.That(storage.TryResolveQualifiedPath(@"C:\root\Graft\some\filepath.txt", out _), Is.False);
        }

        [Test]
        public void RoundtripsQualifiedPathInGraft_BelongingToRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);
            var grafted = builder.CreateNode(@"C:\Other", FileSystemCasing.CasePreservingInsensitive);
            builder.AddGraftPoint(root, "Graft", grafted);
            var storage = builder.BuildHierarchies();

            Assert.That(storage.TryResolveQualifiedPath(@"C:\Other\some\filepath.txt", out var qualifiedPath), Is.True);
            Assert.That(qualifiedPath,
                Is.EqualTo(new QualifiedPath(grafted, RelativePath.CreateFromSegments("some", "filepath.txt")))
                    .Using(default(QualifiedPath.DefaultEqualityComparer)));

            Assert.That(storage.ResolveToFile(qualifiedPath)?.FullName, Is.EqualTo(@"C:\Other\some\filepath.txt"));
        }

        [Test]
        public void RoundtripsAbstractPathInRootWithNoGrafts()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("SimpleRoot", root);
            var storage = builder.BuildHierarchies();

            var qualifiedPath = new QualifiedPath(root, RelativePath.CreateFromSegments("some", "filepath.txt"));
            var expectedAbstractPath = new AbstractPath(storage.NamedRoots.Single(), RelativePath.CreateFromSegments("some", "filepath.txt"));

            var abstractPath = storage.MapToAbstractPath(qualifiedPath);

            Assert.That(abstractPath, Is.EqualTo(expectedAbstractPath).Using(default(AbstractPath.DefaultEqualityComparer)));
            Assert.That(storage.MapFromAbstractPath(abstractPath),
                Is.EqualTo(qualifiedPath)
                    .Using(default(QualifiedPath.DefaultEqualityComparer)));
        }

        [Test]
        public void RoundtripsAbstractPathInGraft_BelongingToRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);
            var grafted = builder.CreateNode(@"C:\Other", FileSystemCasing.CasePreservingInsensitive);
            builder.AddGraftPoint(root, "Graft", grafted);
            var storage = builder.BuildHierarchies();

            var qualifiedPath = new QualifiedPath(grafted, RelativePath.CreateFromSegments("some", "filepath.txt"));
            var expectedAbstractPath = new AbstractPath(storage.NamedRoots.Single(), RelativePath.CreateFromSegments("Graft", "some", "filepath.txt"));

            var abstractPath = storage.MapToAbstractPath(qualifiedPath);

            Assert.That(abstractPath, Is.EqualTo(expectedAbstractPath).Using(default(AbstractPath.DefaultEqualityComparer)));
            Assert.That(storage.MapFromAbstractPath(abstractPath),
                Is.EqualTo(qualifiedPath)
                    .Using(default(QualifiedPath.DefaultEqualityComparer)));
        }

        [Test]
        public void DoesNotMapExcludedPath()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);
            var regex = new GlobToRegexCompiler().CompileRegex("**/.caber/**", FileSystemCasing.CaseSensitive);
            builder.AddFilter(root, new RelativePathMatcher(regex, FilterRule.Exclude));
            var storage = builder.BuildHierarchies();

            var qualifiedPath = new QualifiedPath(root, RelativePath.CreateFromSegments(".caber", "tempfile"));

            Assert.That(storage.MapToAbstractPath(qualifiedPath), Is.Null);
        }

        [Test]
        public void MapsIncludedPath()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);
            builder.AddFilter(root, new RelativePathMatcher(new Regex(".*"), FilterRule.Exclude));
            var regex = new GlobToRegexCompiler().CompileRegex("**/*.txt", FileSystemCasing.CaseSensitive);
            builder.AddFilter(root, new RelativePathMatcher(regex, FilterRule.Include));
            var storage = builder.BuildHierarchies();

            var qualifiedPath = new QualifiedPath(root, RelativePath.CreateFromSegments("test", "log.txt"));

            Assert.That(storage.MapToAbstractPath(qualifiedPath), Is.Not.Null);
        }
    }
}
