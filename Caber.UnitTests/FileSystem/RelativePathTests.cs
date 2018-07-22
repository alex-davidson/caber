using System;
using Caber.FileSystem;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    [TestFixture]
    public class RelativePathTests
    {
        [Test]
        public void CannotCreatePathWithNoSegments()
        {
            Assert.Throws<ArgumentException>(() => RelativePath.CreateFromSegments());
        }

        [Test]
        public void PathUsesNormalSeparator()
        {
            var path = RelativePath.CreateFromSegments("a", "b", "c");
            Assert.That(path.ToString(), Is.EqualTo("a/b/c"));
        }

        [Test]
        public void FilePathIsNotContainer()
        {
            var path = RelativePath.CreateFromSegments("a", "b", "c");
            Assert.That(path.IsContainer(), Is.False);
        }

        [Test]
        public void ContainerPathIsContainer()
        {
            var path = RelativePath.CreateFromSegments("a", "b", "c").AsContainer();
            Assert.That(path.IsContainer(), Is.True);
        }

        [Test]
        public void PathIsEqualToIdenticalCasePath([ValueSource(nameof(ValidFileSystemCasings))] FileSystemCasing casing)
        {
            var comparer = new PathEqualityComparer(casing);
            var pathA = RelativePath.CreateFromSegments("a", "b", "c");
            var pathB = RelativePath.CreateFromSegments("a", "b", "c");

            Assert.That(pathA, Is.EqualTo(pathB).Using<RelativePath>(comparer));
        }

        [Test]
        public void PathIsEqualToDifferentCasePath_WhenPolicyIsCaseInsensitive()
        {
            var comparer = new PathEqualityComparer(FileSystemCasing.CasePreservingInsensitive);
            var pathA = RelativePath.CreateFromSegments("a", "b", "c");
            var pathB = RelativePath.CreateFromSegments("A", "B", "c");

            Assert.That(pathA, Is.EqualTo(pathB).Using<RelativePath>(comparer));
        }

        [Test]
        public void PathIsContainedByParent([ValueSource(nameof(ValidFileSystemCasings))] FileSystemCasing casing)
        {
            var comparer = new PathEqualityComparer(casing);
            var path = RelativePath.CreateFromSegments("a", "b", "c");
            var parentPath = RelativePath.CreateFromSegments("a", "b").AsContainer();

            Assert.That(parentPath.Contains(path, comparer), Is.True);
        }

        [Test]
        public void PathIsContainedByDifferingCaseParent_WhenPolicyIsCaseInsensitive()
        {
            var comparer = new PathEqualityComparer(FileSystemCasing.CasePreservingInsensitive);
            var path = RelativePath.CreateFromSegments("A", "B", "c");
            var parentPath = RelativePath.CreateFromSegments("a", "b").AsContainer();

            Assert.That(parentPath.Contains(path, comparer), Is.True);
        }

        [Test]
        public void PathDoesNotContainParent([ValueSource(nameof(ValidFileSystemCasings))] FileSystemCasing casing)
        {
            var comparer = new PathEqualityComparer(casing);
            var path = RelativePath.CreateFromSegments("a", "b", "c");
            var parentPath = RelativePath.CreateFromSegments("a", "b");

            Assert.That(path.Contains(parentPath, comparer), Is.False);
            Assert.That(path.AsContainer().Contains(parentPath, comparer), Is.False);
        }

        [Test]
        public void PathDoesNotContainSibling([ValueSource(nameof(ValidFileSystemCasings))] FileSystemCasing casing)
        {
            var comparer = new PathEqualityComparer(casing);
            var path = RelativePath.CreateFromSegments("a", "b", "c");
            var siblingPath = RelativePath.CreateFromSegments("a", "b", "d");

            Assert.That(path.Contains(siblingPath, comparer), Is.False);
            Assert.That(path.AsContainer().Contains(siblingPath, comparer), Is.False);
        }

        [Test]
        public void PathDoesNotContainSelf([ValueSource(nameof(ValidFileSystemCasings))] FileSystemCasing casing)
        {
            var comparer = new PathEqualityComparer(casing);
            var path = RelativePath.CreateFromSegments("a", "b", "c");

            Assert.That(path.Contains(path, comparer), Is.False);
            Assert.That(path.AsContainer().Contains(path, comparer), Is.False);
            Assert.That(path.AsContainer().Contains(path.AsContainer(), comparer), Is.False);
        }

        public static FileSystemCasing[] ValidFileSystemCasings = {
            FileSystemCasing.CaseSensitive,
            FileSystemCasing.CasePreservingInsensitive
        };
    }
}
