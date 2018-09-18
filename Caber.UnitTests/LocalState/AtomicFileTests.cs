using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caber.FileSystem;
using Caber.LocalState;
using Caber.UnitTests.TestHelpers;
using NUnit.Framework;

namespace Caber.UnitTests.LocalState
{
    [TestFixture]
    public class AtomicFileTests
    {
        [Test]
        public void CanWrite()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut.Write(42);
            }
        }

        [Test]
        public void CanDeleteWrittenValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut.Write(42);
                sut.Delete();

                Assert.That(sut.Read(), Is.EqualTo(default(int)));
            }
        }

        [Test]
        public void CanReadWrittenValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut.Write(42);

                var value = sut.Read();

                Assert.That(value, Is.EqualTo(42));
            }
        }

        [Test]
        public void CanReplaceWrittenValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut.Write(42);
                sut.Write(23);

                var value = sut.Read();

                Assert.That(value, Is.EqualTo(23));
            }
        }

        [Test]
        public void CanAccessViaMultipleInstances()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut1 = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                var sut2 = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                var sut3 = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut1.Write(42);
                sut2.Write(17);
                sut3.Write(23);

                var value = sut2.Read();

                Assert.That(value, Is.EqualTo(23));
            }
        }

        [Test]
        public void FailedWriteDoesNotOverwriteExistingValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut.Write(42);
                Assume.That(sut.Read(), Is.EqualTo(42));

                var failing = new AtomicFile<int>(container.File("key-path"), new RandomlyFailingSerialiser<int>(new StubSerialiser(), 1));
                Assume.That(() => failing.Write(23), Throws.InstanceOf<IOException>());

                Assert.That(sut.Read(), Is.EqualTo(42));
            }
        }

        [Test]
        public void MultipleFailedWritesDoNotOverwriteExistingValue()
        {
            using (var container = TemporaryDirectory.CreateNew())
            {
                var sut = new AtomicFile<int>(container.File("key-path"), new StubSerialiser());
                sut.Write(42);
                Assume.That(sut.Read(), Is.EqualTo(42));

                var failing = new AtomicFile<int>(container.File("key-path"), new RandomlyFailingSerialiser<int>(new StubSerialiser(), 1));
                Assume.That(() => failing.Write(23), Throws.InstanceOf<IOException>());
                Assume.That(() => failing.Write(17), Throws.InstanceOf<IOException>());
                Assume.That(() => failing.Write(128), Throws.InstanceOf<IOException>());

                Assert.That(sut.Read(), Is.EqualTo(42));
            }
        }

        [Test, Repeat(20)]
        public async Task FuzzConcurrentAccess()
        {
            const int threadCount = 5;
            const int iterationCountPerThread = 25;

            var writtenValues = new List<int>();
            var random = new Random();
            var failures = new List<Exception>();

            using (var container = TemporaryDirectory.CreateNew())
            {
                var path = container.File("key-path");
                await ConcurrentOperations.Run(threadCount, () => {
                    var sut = new AtomicFile<int>(path, new RandomlyFailingSerialiser<int>(new StubSerialiser(), 0.05));
                    for (var i = 0; i < iterationCountPerThread; i++)
                    {
                        try
                        {
                            var value = random.Next(100);
                            sut.Write(value);
                            lock (writtenValues) writtenValues.Add(value);
                        }
                        catch (FileLockedException)
                        {
                            // We expect locking conflicts to represent the bulk of failures.
                        }
                        catch (Exception ex)
                        {
                            lock (failures) failures.Add(ex);
                        }
                        try
                        {
                            sut.Read();
                        }
                        catch (Exception) { }
                    }
                });

                var finalValue = new AtomicFile<int>(path, new StubSerialiser()).Read();

                // At least one write should succeed.
                Assume.That(writtenValues.Count, Is.GreaterThan(1));

                // Due to races it's not guaranteed that the last entry in the list is the last one successfully written.
                // However, this should be vanishingly unlikely because it would require that another Write call ran to
                // completion between `sut.Write(value);` and `lock (writtenValues)`.
                Assert.That(finalValue, Is.EqualTo(writtenValues.Last()));
            }
        }

        /// <summary>
        /// Serialiser decorator which randomly throws IOExceptions.
        /// </summary>
        private class RandomlyFailingSerialiser<T> : IStateSerialiser<T>
        {
            private readonly IStateSerialiser<T> inner;
            private readonly double failureProbability;
            private static readonly Random Random = new Random();

            public RandomlyFailingSerialiser(IStateSerialiser<T> inner, double failureProbability)
            {
                this.inner = inner;
                this.failureProbability = failureProbability;
            }

            public void Write(Stream stream, T value)
            {
                var shouldFail = Random.NextDouble() < failureProbability;
                var failBefore = Random.NextDouble() < 0.5;

                if (shouldFail && failBefore) throw new IOException("Random failure before write");
                inner.Write(stream, value);
                if (shouldFail && !failBefore) throw new IOException("Random failure after write");
            }

            public T Read(Stream stream)
            {
                if (Random.NextDouble() < failureProbability) throw new IOException("Random failure during read");
                return inner.Read(stream);
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
