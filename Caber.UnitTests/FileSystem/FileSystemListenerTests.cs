using System;
using System.Collections.Generic;
using System.IO;
using Caber.FileSystem;
using Caber.Routing;
using Caber.UnitTests.TestHelpers;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    [TestFixture, Timeout(5000)]
    public class FileSystemListenerTests
    {
        [Test, Repeat(10)]
        public void CreatedFileIsAddedToPool()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var root = new LocalRoot(new Uri(temp.FullPath), FileSystemCasing.CasePreservingInsensitive);
                var pool = new FileSystemPathPool();

                var filePath = Path.Combine(temp.FullPath, "file");

                using (var listener = new FileSystemListener(root, pool))
                {
                    File.WriteAllText(filePath, "");

                    listener.Notified.WaitOne();
                    listener.Flush();
                }

                var contents = GetPoolContents(pool);
                Assert.That(contents, Has.Member(filePath));
            }
        }

        [Test, Repeat(10)]
        public void ChangedFileIsAddedToPool()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var root = new LocalRoot(new Uri(temp.FullPath), FileSystemCasing.CasePreservingInsensitive);
                var pool = new FileSystemPathPool();

                var filePath = Path.Combine(temp.FullPath, "file");
                File.WriteAllText(filePath, "");

                using (var listener = new FileSystemListener(root, pool))
                {
                    File.AppendAllText(filePath, "test");

                    listener.Notified.WaitOne();
                    listener.Flush();
                }

                var contents = GetPoolContents(pool);
                Assert.That(contents, Has.Member(filePath));
            }
        }

        [Test, Repeat(10)]
        public void RenamedFileNewPathIsAddedToPool()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var root = new LocalRoot(new Uri(temp.FullPath), FileSystemCasing.CasePreservingInsensitive);
                var pool = new FileSystemPathPool();

                var oldFilePath = Path.Combine(temp.FullPath, "old-file");
                File.WriteAllText(oldFilePath, "");
                var newFilePath = Path.Combine(temp.FullPath, "new-file");

                using (var listener = new FileSystemListener(root, pool))
                {
                    File.Move(oldFilePath, newFilePath);

                    listener.Notified.WaitOne();
                    listener.Flush();
                }

                var contents = GetPoolContents(pool);
                Assert.That(contents, Has.Member(newFilePath));
            }
        }

        [Test, Repeat(10)]
        public void CreatedDirectoryIsNotAddedToPool()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var root = new LocalRoot(new Uri(temp.FullPath), FileSystemCasing.CasePreservingInsensitive);
                var pool = new FileSystemPathPool();

                var directoryPath = Path.Combine(temp.FullPath, "directory");

                using (var listener = new FileSystemListener(root, pool))
                {
                    Directory.CreateDirectory(directoryPath);

                    listener.Notified.WaitOne(10);
                    listener.Flush();
                }

                var contents = GetPoolContents(pool);
                Assert.That(contents, Has.No.Member(directoryPath));
            }
        }

        [Test, Repeat(10)]
        public void RenamedDirectoryOldAndNewPathsAreNotAddedToPool()
        {
            using (var temp = TemporaryDirectory.CreateNew())
            {
                var root = new LocalRoot(new Uri(temp.FullPath), FileSystemCasing.CasePreservingInsensitive);
                var pool = new FileSystemPathPool();

                var oldDirectoryPath = Path.Combine(temp.FullPath, "old-directory");
                Directory.CreateDirectory(oldDirectoryPath);
                var newDirectoryPath = Path.Combine(temp.FullPath, "new-directory");

                using (var listener = new FileSystemListener(root, pool))
                {
                    Directory.Move(oldDirectoryPath, newDirectoryPath);

                    listener.Notified.WaitOne(10);
                    listener.Flush();
                }

                var contents = GetPoolContents(pool);
                Assert.That(contents, Has.No.Member(oldDirectoryPath));
                Assert.That(contents, Has.No.Member(newDirectoryPath));
            }
        }

        private ICollection<string> GetPoolContents(FileSystemPathPool pool)
        {
            var paths = new List<string>();
            while (pool.TryTake(out var path))
            {
                paths.Add(path);
            }
            return paths;
        }
    }
}
