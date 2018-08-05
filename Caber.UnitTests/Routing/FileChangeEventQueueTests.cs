using System;
using System.Collections.Generic;
using System.Linq;
using Caber.FileSystem;
using Caber.Retry;
using Caber.Routing;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Caber.UnitTests.Routing
{
    [TestFixture]
    public class FileChangeEventQueueTests
    {
        [Test]
        public void IgnoresEnqueueingOfEventsAlreadyQueued()
        {
            var queue = new FileChangeEventQueue();
            queue.Enqueue(CreateEvent("A"));
            queue.Enqueue(CreateEvent("B"));
            queue.Enqueue(CreateEvent("A"));
            queue.Enqueue(CreateEvent("A"));

            var items = ConsumeQueue(queue);
            Assert.That(items.Select(i => i.AbstractPath),
                Is.EquivalentTo(new [] {
                    CreateEvent("A").AbstractPath,
                    CreateEvent("B").AbstractPath
                }).Using(default(AbstractPath.DefaultEqualityComparer)));
        }

        [Test]
        public void AllowsRequeueingOfDequeuedEvent()
        {
            var queue = new FileChangeEventQueue();
            queue.Enqueue(CreateEvent("A"));

            Assert.That(queue.TryDequeue(out var item), Is.True);
            queue.Enqueue(item);

            Assert.That(queue.TryDequeue(out var requeued), Is.True);

            Assert.That(item, Is.EqualTo(requeued));
        }

        [Test]
        public void NotifiesWhenEventIsQueued()
        {
            var queue = new FileChangeEventQueue();
            Assume.That(queue.Queued.WaitOne(0), Is.False);

            queue.Enqueue(CreateEvent("A"));

            Assert.That(queue.Queued.WaitOne(0), Is.True);
        }

        [Test]
        public void RetryOfAlreadyEnqueuedEventPreventsThatEventFromBeingDequeued()
        {
            var clock = new MockClock(Baseline);
            var queue = new FileChangeEventQueue() { Clock = clock };
            queue.Enqueue(CreateEvent("A"));

            var token = new RetryToken(Baseline + TimeSpan.FromMinutes(1));
            queue.Enqueue(CreateEvent("A"), token);

            Assert.That(queue.TryDequeue(out _), Is.False);
        }

        [Test]
        public void RetryOfAlreadyEnqueuedEventRetainsThatEventsPositionInTheQueue()
        {
            var clock = new MockClock(Baseline);
            var queue = new FileChangeEventQueue() { Clock = clock };
            var blockedItem = CreateEvent("A");
            queue.Enqueue(blockedItem);
            queue.Enqueue(CreateEvent("B"));

            var token = new RetryToken(Baseline + TimeSpan.FromMinutes(1));
            queue.Enqueue(CreateEvent("A"), token);

            clock.Advance();

            Assert.That(queue.TryDequeue(out var item), Is.True);
            Assert.That(item, Is.EqualTo(blockedItem));
        }

        [Test]
        public void MultipleRetryOfAlreadyEnqueuedEventUsesEarliestRetry()
        {
            var clock = new MockClock(Baseline);
            var queue = new FileChangeEventQueue() { Clock = clock };
            queue.Enqueue(CreateEvent("A"));

            var firstToken = new RetryToken(Baseline + TimeSpan.FromMinutes(1));
            var secondToken = new RetryToken(Baseline + TimeSpan.FromMinutes(3));

            queue.Enqueue(CreateEvent("A"), secondToken);
            queue.Enqueue(CreateEvent("A"), firstToken);

            clock.Advance(TimeSpan.FromMinutes(2));

            Assert.That(queue.TryDequeue(out var item), Is.True);
        }

        private IList<FileChangeEvent> ConsumeQueue(FileChangeEventQueue queue)
        {
            var list = new List<FileChangeEvent>();
            while (queue.TryDequeue(out var item)) list.Add(item);
            return list;
        }

        private static readonly DateTimeOffset Baseline = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero);
        private static readonly LocalRoot Root = new LocalRoot(new Uri(@"c:\temp\"), FileSystemCasing.CasePreservingInsensitive);
        private FileChangeEvent CreateEvent(string identifier)
        {
            var relativePath = RelativePath.CreateFromSegments(identifier);
            return new FileChangeEvent(
                Baseline,
                new QualifiedPath(Root, relativePath),
                new AbstractPath(new NamedRoot("test", Root), relativePath));
        }
    }
}
