using System;
using System.IO;
using Caber.FileSystem.Windows;
using Moq;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem.Windows
{
    [TestFixture]
    public class WindowsPathCanonicaliserTests
    {
        private const string Root = @"c:\root";

        [Test]
        public void TryCanonicaliseRelative_When_RootDoesNotExist_LeavesPartsUnchanged()
        {
            var parts = new [] { "A", "B", "C" };
            var expectedParts = new [] { "A", "B", "C" };

            var api = Mock.Of<WindowsFileSystemApi.Internal>(a => a.Lookup(Root) == null);
            var sut = new WindowsPathCanonicaliser();

            var exists = sut.TryCanonicaliseRelative(api, Root, parts);

            Assert.That(exists, Is.False);
            Assert.That(parts, Is.EqualTo(expectedParts));
        }

        [Test]
        public void TryCanonicaliseRelative_When_AllPartsExistWithSameCase_ReturnsTrueAndOriginalCasing()
        {
            var parts = new [] { "a", "b", "c" };
            var expectedParts = new [] { "a", "b", "c" };

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), Root, parts);
            var sut = new WindowsPathCanonicaliser();

            var exists = sut.TryCanonicaliseRelative(api, Root, parts);

            Assert.That(exists, Is.True);
            Assert.That(parts, Is.EqualTo(expectedParts));
        }

        [Test]
        public void TryCanonicaliseRelative_When_AllPartsExistWithDifferentCasing_ReturnsTrueAndCorrectCasing()
        {
            var parts = new [] { "A", "B", "C" };
            var expectedParts = new [] { "a", "b", "c" };

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), Root, expectedParts);
            var sut = new WindowsPathCanonicaliser();

            var exists = sut.TryCanonicaliseRelative(api, Root, parts);

            Assert.That(exists, Is.True);
            Assert.That(parts, Is.EqualTo(expectedParts));
        }

        [Test]
        public void TryCanonicaliseRelative_When_SomePartsExistWithDifferentCasing_ReturnsFalseAndCorrectCasingForExistingParts()
        {
            var parts = new [] { "A", "B", "C" };
            var expectedParts = new [] { "a", "b", "C" };

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), Root, "a", "b");
            var sut = new WindowsPathCanonicaliser();

            var exists = sut.TryCanonicaliseRelative(api, Root, parts);

            Assert.That(exists, Is.False);
            Assert.That(parts, Is.EqualTo(expectedParts));
        }

        [Test]
        public void CanonicaliseAbsolute_When_RootDoesNotExist_LeavesPartsUnchanged()
        {
            var originalPath = @"e:\a\b\c";
            var expectedPath = @"e:\a\b\c";

            var api = Mock.Of<WindowsFileSystemApi.Internal>(a => a.Lookup(@"e:\") == null);
            var sut = new WindowsPathCanonicaliser();

            var result = sut.CanonicaliseAbsolute(api, originalPath);

            Assert.That(result.LocalPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void CanonicaliseAbsolute_When_AllPartsExistWithSameCase_ReturnsOriginalCasing()
        {
            var originalPath = @"e:\a\b\c";
            var expectedPath = @"e:\a\b\c";

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), @"e:\", "a", "b", "c");
            var sut = new WindowsPathCanonicaliser();

            var result = sut.CanonicaliseAbsolute(api, originalPath);

            Assert.That(result.LocalPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void CanonicaliseAbsolute_When_AllPartsExistWithDifferentCasing_ReturnsCorrectCasing()
        {
            var originalPath = @"e:\A\B\C";
            var expectedPath = @"e:\a\b\c";

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), @"e:\", "a", "b", "c");
            var sut = new WindowsPathCanonicaliser();

            var result = sut.CanonicaliseAbsolute(api, originalPath);

            Assert.That(result.LocalPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void CanonicaliseAbsolute_When_SomePartsExistWithDifferentCasing_ReturnsCorrectCasingForExistingParts()
        {
            var originalPath = @"e:\A\B\C";
            var expectedPath = @"e:\a\b\C";

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), @"e:\", "a", "b");
            var sut = new WindowsPathCanonicaliser();

            var result = sut.CanonicaliseAbsolute(api, originalPath);

            Assert.That(result.LocalPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void CanonicaliseAbsolute_DirectoryPath_ReturnsDirectoryUri()
        {
            var originalPath = @"e:\a\b\c\";
            var expectedPath = @"e:\a\b\c\";

            var api = Mock.Of<WindowsFileSystemApi.Internal>();
            MockHierarchy(Mock.Get(api), @"e:\", "a", "b", "c");
            var sut = new WindowsPathCanonicaliser();

            var result = sut.CanonicaliseAbsolute(api, originalPath);

            Assert.That(result.LocalPath, Is.EqualTo(expectedPath));
        }

        private static void MockHierarchy(Mock<WindowsFileSystemApi.Internal> mock, string root, params string[] parts)
        {
            var current = new DirectoryInfo(root);
            mock.Setup(x => x.Lookup(root)).Returns(current);
            foreach (var part in parts)
            {
                var parentPart = current;
                current = new DirectoryInfo(Path.Combine(current.FullName, part));
                mock.Setup(x => x.Lookup(parentPart, It.Is<string>(s => StringComparer.OrdinalIgnoreCase.Equals(s, part)))).Returns(current);
            }
        }
    }
}
