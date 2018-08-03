using System;
using Caber.Retry;
using NUnit.Framework;

namespace Caber.UnitTests.Retry
{
    [TestFixture]
    public class RetryGroupTests
    {
        private static readonly DateTimeOffset Baseline = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void ParallelCombinationUsesEarliestToken()
        {
            var group = new RetryGroup();

            group.Parallel().RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(2)));
            group.Parallel().RetryWith(new RetryToken(Baseline));
            group.Parallel().RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(1)));

            Assert.That(group.GetToken(), Is.EqualTo(new RetryToken(Baseline)));
        }

        [Test]
        public void SequenceCombinationUsesLatestToken()
        {
            var group = new RetryGroup();

            group.RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(2)));
            group.RetryWith(new RetryToken(Baseline));
            group.RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(1)));

            Assert.That(group.GetToken(), Is.EqualTo(new RetryToken(Baseline + TimeSpan.FromMinutes(2))));
        }

        [Test]
        public void CombinationOfParallelCombinationsUsesEarliestAggregateOfEachParallelSequence()
        {
            var group = new RetryGroup();
            var parallelA = group.Parallel();
            var parallelB = group.Parallel();

            parallelA.RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(2)));
            parallelA.RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(1)));

            parallelB.RetryWith(new RetryToken(Baseline));
            parallelB.RetryWith(new RetryToken(Baseline + TimeSpan.FromMinutes(3)));

            Assert.That(group.GetToken(), Is.EqualTo(new RetryToken(Baseline + TimeSpan.FromMinutes(2))));
        }
    }
}
