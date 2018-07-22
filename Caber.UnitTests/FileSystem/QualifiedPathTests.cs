using System;
using Caber.FileSystem;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    [TestFixture]
    public class QualifiedPathTests
    {
        [Test]
        public void QualifiedPathsWithDifferentRoots_AreNotEqual([ValueSource(nameof(ValidFileSystemCasings))] FileSystemCasing casing)
        {
            var relativePath = RelativePath.CreateFromSegments("a", "b", "c");
            var a = new QualifiedPath(new LocalRoot(new Uri(@"C:\"), casing), relativePath);
            var b = new QualifiedPath(new LocalRoot(new Uri(@"E:\"), casing), relativePath);

            Assert.That(a, Is.Not.EqualTo(b).Using(default(QualifiedPath.DefaultEqualityComparer)));
        }

        [Test]
        public void QualifiedPathsWithSameCaseSensitiveRoot_AndDifferingCase_AreNotEqual()
        {
            var root = new LocalRoot(new Uri(@"C:\"), FileSystemCasing.CaseSensitive);
            var a = new QualifiedPath(root, RelativePath.CreateFromSegments("a", "b", "c"));
            var b = new QualifiedPath(root, RelativePath.CreateFromSegments("A", "B", "C"));

            Assert.That(a, Is.Not.EqualTo(b).Using(default(QualifiedPath.DefaultEqualityComparer)));
        }

        [Test]
        public void QualifiedPathsWithSameCaseSensitiveRoot_AndSameCase_AreEqual()
        {
            var root = new LocalRoot(new Uri(@"C:\"), FileSystemCasing.CaseSensitive);
            var a = new QualifiedPath(root, RelativePath.CreateFromSegments("a", "b", "c"));
            var b = new QualifiedPath(root, RelativePath.CreateFromSegments("a", "b", "c"));

            Assert.That(a, Is.EqualTo(b).Using(default(QualifiedPath.DefaultEqualityComparer)));
        }

        [Test]
        public void QualifiedPathsWithSameCaseInsensitiveRoot_AndDifferingCase_AreEqual()
        {
            var root = new LocalRoot(new Uri(@"C:\"), FileSystemCasing.CasePreservingInsensitive);
            var a = new QualifiedPath(root, RelativePath.CreateFromSegments("a", "b", "c"));
            var b = new QualifiedPath(root, RelativePath.CreateFromSegments("A", "B", "C"));

            Assert.That(a, Is.EqualTo(b).Using(default(QualifiedPath.DefaultEqualityComparer)));
        }

        public static FileSystemCasing[] ValidFileSystemCasings = {
            FileSystemCasing.CaseSensitive,
            FileSystemCasing.CasePreservingInsensitive
        };
    }
}
