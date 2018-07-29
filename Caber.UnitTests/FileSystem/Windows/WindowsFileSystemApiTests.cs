using System;
using Caber.FileSystem;
using Caber.FileSystem.Windows;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem.Windows
{
    [TestFixture, Platform("Win")]
    public class WindowsFileSystemApiTests
    {
        [Test]
        public void CanCreateLocalRootFromDirectoryPath()
        {
            var api = new WindowsFileSystemApi();
            var root = api.CreateStorageRoot(@"C:\temp\root-dir\", FileSystemCasing.Unspecified);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Casing, Is.Not.EqualTo(FileSystemCasing.Unspecified));
        }

        [Test]
        public void CannotCreateLocalRootFromFilePath()
        {
            var api = new WindowsFileSystemApi();
            Assert.Throws<ArgumentException>(() => api.CreateStorageRoot(@"C:\temp\root-file", FileSystemCasing.Unspecified), "Not a directory path");
        }
    }
}
