using System.Collections.Generic;
using System.Linq;
using Caber.Routing;
using NUnit.Framework;

namespace Caber.UnitTests.Routing
{
    [TestFixture]
    public class FileSystemPathPoolTests
    {
        [Test]
        public void CanAddPath()
        {
            var pool = new FileSystemPathPool();
            Assert.DoesNotThrow(() => pool.Add(@"c:\temp\file"));
        }

        [Test]
        public void CanAddSetOfPaths()
        {
            var pool = new FileSystemPathPool();
            var set = new HashSet<string> {
                @"c:\temp\file",
                @"c:\temp\subdir\file"
            };
            Assert.DoesNotThrow(() => pool.Add(set));
        }

        [Test]
        public void NotifiesWhenPathIsQueued()
        {
            var pool = new FileSystemPathPool();
            Assume.That(pool.Queued.WaitOne(0), Is.False);

            pool.Add(@"c:\temp\file");

            Assert.That(pool.Queued.WaitOne(0), Is.True);
        }

        [Test]
        public void NotifiesWhenSetOfPathsIsQueued()
        {
            var pool = new FileSystemPathPool();
            Assume.That(pool.Queued.WaitOne(0), Is.False);

            var set = new HashSet<string> {
                @"c:\temp\file",
                @"c:\temp\subdir\file"
            };
            pool.Add(set);

            Assert.That(pool.Queued.WaitOne(0), Is.True);
        }

        [Test]
        public void CanTakeQueuedPath()
        {
            var pool = new FileSystemPathPool();
            pool.Add(@"c:\temp\file");

            Assert.That(pool.TryTake(out var path), Is.True);
            Assert.That(path, Is.EqualTo(@"c:\temp\file"));
        }

        [Test]
        public void CanTakeQueuedSetOfPaths()
        {
            var pool = new FileSystemPathPool();
            var set = new HashSet<string> {
                @"c:\temp\file",
                @"c:\temp\subdir\file"
            };
            pool.Add(set);

            Assert.That(pool.TryTake(out var path1), Is.True);
            Assert.That(pool.TryTake(out var path2), Is.True);
            Assume.That(pool.TryTake(out _), Is.False);

            Assert.That(new [] { path1, path2 }, Is.EquivalentTo(set));
        }

        [Test]
        public void CannotTakeFromEmptyPool()
        {
            var pool = new FileSystemPathPool();
            Assert.That(pool.TryTake(out _), Is.False);
        }

        [Test]
        public void PathsDifferingOnlyByCaseMayBeTakenSeparately()
        {
            var pool = new FileSystemPathPool();
            var set = new HashSet<string> {
                @"c:\temp\file",
                @"c:\TEMP\file"
            };
            pool.Add(set);

            Assert.That(pool.TryTake(out var path1), Is.True);
            Assert.That(pool.TryTake(out var path2), Is.True);
            Assume.That(pool.TryTake(out _), Is.False);

            Assert.That(new [] { path1, path2 }, Is.EquivalentTo(set.ToArray()));
        }
    }
}
