using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caber.Util;
using NUnit.Framework;

namespace Caber.UnitTests.Util
{
    [TestFixture]
    public class DeadlineTests
    {
        [Test]
        public void RecordedErrorIsInnerExceptionOfTimeout()
        {
            var clock = new MockClock(DateTimeOffset.Now);
            var deadline = new Deadline(clock, TimeSpan.FromMinutes(5));
            var error = new OutOfMemoryException();
            deadline.OnError(error);

            Assert.That(deadline.Timeout().InnerException, Is.SameAs(error));
        }

        [Test]
        public void IfNoErrorIsRecordedThenTimeoutHasNoInnerException()
        {
            var clock = new MockClock(DateTimeOffset.Now);
            var deadline = new Deadline(clock, TimeSpan.FromMinutes(5));

            Assert.That(deadline.Timeout().InnerException, Is.Null);
        }

        [Test]
        public void IsNotExpiredBeforeLimitExpires()
        {
            var clock = new MockClock(DateTimeOffset.Now);
            var deadline = new Deadline(clock, TimeSpan.FromMinutes(5));
            clock.Advance(TimeSpan.FromMinutes(4));

            Assert.That(deadline.HasExpired, Is.False);
        }

        [Test]
        public void IsExpiredAfterLimitExpires()
        {
            var clock = new MockClock(DateTimeOffset.Now);
            var deadline = new Deadline(clock, TimeSpan.FromMinutes(5));
            clock.Advance(TimeSpan.FromMinutes(5));

            Assert.That(deadline.HasExpired, Is.True);
        }
    }
}
