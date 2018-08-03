using System;
using System.Threading;
using System.Threading.Tasks;
using Caber.Util;

namespace Caber.Retry
{
    public struct RetryToken : IEquatable<RetryToken>, IComparable<RetryToken>
    {
        public static readonly RetryToken None = default;

        private readonly DateTimeOffset when;

        public RetryToken(DateTimeOffset when)
        {
            this.when = when;
        }

        public bool HasExpired(IClock clock) => clock.Now >= when;
        public Task WaitAsync(IClock clock, CancellationToken token = default) => clock.WaitAsync(when - clock.Now, token);

        /// <summary>
        /// Produces a RetryToken which completes when both inputs complete.
        /// </summary>
        public static RetryToken operator &(RetryToken a, RetryToken b)
        {
            if (a.Equals(None)) return b;
            if (b.Equals(None)) return a;
            return a.when > b.when ? a : b;
        }

        /// <summary>
        /// Produces a RetryToken which completes when either input completes.
        /// </summary>
        public static RetryToken operator |(RetryToken a, RetryToken b)
        {
            if (a.Equals(None)) return b;
            if (b.Equals(None)) return a;
            return a.when < b.when ? a : b;
        }

        public bool Equals(RetryToken other) => when.Equals(other.when);
        public override int GetHashCode() => when.GetHashCode();
        public int CompareTo(RetryToken other) => when.CompareTo(other.when);
    }
}
