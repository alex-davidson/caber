using System;
using Caber.Retry;
using NUnit.Framework;

namespace Caber.UnitTests.Retry
{
    [TestFixture]
    public class RetryTokenTests
    {
        private static readonly DateTimeOffset Baseline = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void LogicalAndReturnsLaterToken()
        {
            var earlier = new RetryToken(Baseline);
            var later = new RetryToken(Baseline + TimeSpan.FromMinutes(1));

            Assert.That(earlier & later, Is.EqualTo(later));
        }

        [Test]
        public void LogicalAndPrefersNotImmediateToken()
        {
            var token = new RetryToken(Baseline);

            Assert.That(token & RetryToken.None, Is.EqualTo(token));
        }

        [Test]
        public void LogicalOrReturnsEarlierToken()
        {
            var earlier = new RetryToken(Baseline);
            var later = new RetryToken(Baseline + TimeSpan.FromMinutes(1));

            Assert.That(earlier | later, Is.EqualTo(earlier));
        }

        [Test]
        public void LogicalOrPrefersNotImmediateToken()
        {
            var token = new RetryToken(Baseline);

            Assert.That(token | RetryToken.None, Is.EqualTo(token));
        }

        [Test]
        public void EquivalentTokensAreEqual()
        {
            var a = new RetryToken(Baseline);
            var b = new RetryToken(Baseline);

            Assert.That(a, Is.EqualTo(b));
        }
    }
}
