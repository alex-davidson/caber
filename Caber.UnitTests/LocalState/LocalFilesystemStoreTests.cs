using System;
using System.IO;
using Caber.LocalState;
using Caber.Logging;
using Caber.UnitTests.TestHelpers;
using Moq;
using NUnit.Framework;

namespace Caber.UnitTests.LocalState
{
    [TestFixture]
    public class LocalFilesystemStoreTests
    {
        [Test]
        public void CanWrite()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new LocalFilesystemStore(container.FullPath, new StubSerialiserProvider());
                sut.SetValue("key", 42);
            }
        }

        [Test]
        public void CanDeleteWrittenValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new LocalFilesystemStore(container.FullPath, new StubSerialiserProvider());
                sut.SetValue("key", 42);
                sut.RemoveKey("key");

                Assert.That(sut.GetValue<int>("key"), Is.EqualTo(default(int)));
            }
        }

        [Test]
        public void CanReadWrittenValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new LocalFilesystemStore(container.FullPath, new StubSerialiserProvider());
                sut.SetValue("key", 42);

                var value = sut.GetValue<int>("key");

                Assert.That(value, Is.EqualTo(42));
            }
        }

        [Test]
        public void CanReplaceWrittenValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new LocalFilesystemStore(container.FullPath, new StubSerialiserProvider());
                sut.SetValue("key", 42);
                sut.SetValue("key", 23);

                var value = sut.GetValue<int>("key");

                Assert.That(value, Is.EqualTo(23));
            }
        }

        [Test]
        public void RecordsFailureLeadingToTimeoutInDiagnosticLog()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var clock = new MockClock(DateTimeOffset.Now);
                var log = Mock.Of<IDiagnosticsLog>();
                var serialiserProvider = new FailingActionSerialiserProvider(() => {
                    clock.Advance(TimeSpan.FromSeconds(10));
                    throw new OutOfMemoryException();
                });

                var sut = new LocalFilesystemStore(container.FullPath, serialiserProvider, log)
                {
                    Clock = clock,
                    Timeout = TimeSpan.FromSeconds(5)
                };

                Assume.That(() => sut.SetValue("key", 42), Throws.InstanceOf<TimeoutException>());

                Mock.Get(log).Verify(l => l.Info(It.Is<LocalStoreTimeoutEvent>(t => t.Exception.Type.FullName == typeof(OutOfMemoryException).FullName)));
            }
        }

        private class FailingActionSerialiserProvider : IStateSerialiserProvider
        {
            private readonly Action action;

            public FailingActionSerialiserProvider(Action action)
            {
                this.action = action;
            }

            public IStateSerialiser<T> Create<T>() => new FailingActionSerialiser<T>(action);

            private class FailingActionSerialiser<T> : IStateSerialiser<T>
            {
                private readonly Action action;

                public FailingActionSerialiser(Action action)
                {
                    this.action = action;
                }

                public void Write(Stream stream, T value)
                {
                    action();
                }

                public T Read(Stream stream)
                {
                    action();
                    return default;
                }
            }
        }

        private class StubSerialiserProvider : IStateSerialiserProvider
        {
            public IStateSerialiser<T> Create<T>()
            {
                return new StubSerialiser() as IStateSerialiser<T> ?? Mock.Of<IStateSerialiser<T>>();
            }
        }

        /// <summary>
        /// Simple integer serialiser which records the value as the length of the stream.
        /// This ensures that the serialised values will vary in size.
        /// </summary>
        private class StubSerialiser : IStateSerialiser<int>
        {
            public void Write(Stream stream, int value)
            {
                var buffer = new byte[value];
                stream.Write(buffer, 0, buffer.Length);
            }

            public int Read(Stream stream)
            {
                stream.ReadByte();  // Check that we can actually read from the stream.
                return (int)stream.Length;
            }
        }
    }
}
