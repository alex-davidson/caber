using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.FileSystem.Windows;
using Caber.UnitTests.TestHelpers;
using Caber.Util;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    [TestFixture]
    public class FileSnapshotServiceTests
    {
        [Test]
        public async Task ReturnsNullSnapshotIfFileDoesNotExist()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var storage = BuildSimpleHierarchy(temp);
                var service = new FileSnapshotService(storage);

                var osPath = Path.Combine(temp.FullPath, "missing-file");
                storage.TryResolveQualifiedPath(osPath, out var qualifiedPath);

                var snapshot = await service.GetSnapshot(qualifiedPath, DateTimeOffset.Now);

                Assert.That(snapshot, Is.Null);
            }
        }

        [Test]
        public async Task ReturnsLengthAndHashIfFileExists()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var storage = BuildSimpleHierarchy(temp);
                var service = new FileSnapshotService(storage);

                var osPath = Path.Combine(temp.FullPath, "file");
                File.WriteAllText(osPath, "contents");
                storage.TryResolveQualifiedPath(osPath, out var qualifiedPath);

                var snapshot = await service.GetSnapshot(qualifiedPath, DateTimeOffset.Now);

                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot.Length, Is.EqualTo(8));
                Assert.That(snapshot.Sha256, Is.Not.EqualTo(Hash.None));
            }
        }

        private static StorageHierarchies BuildSimpleHierarchy(TemporaryDirectory temp)
        {
            var builder = new StorageHierarchiesBuilder(new WindowsFileSystemApi());
            var root = builder.CreateNode(temp.FullPath, FileSystemCasing.Unspecified);
            builder.AddNamedRoot("root", root);
            return builder.BuildHierarchies();
        }
    }
}
