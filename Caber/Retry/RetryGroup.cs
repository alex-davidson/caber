using System.Diagnostics;

namespace Caber.Retry
{
    public class RetryGroup : IRetryParent, IRetryCollector
    {
        private readonly RetryableSequence sequence;
        private RetryToken token;

        public RetryGroup()
        {
            sequence = new RetryableSequence(this, 0);
        }

        public IRetryCollector Parallel() => sequence.Parallel();

        public bool RetryRequested => default(RetryToken).Equals(token);

        void IRetryParent.Notify(RetryToken newToken, int i)
        {
            Debug.Assert(i == 0);
            token = newToken;
        }

        public RetryToken GetToken() => token;

        public void RetryWith(RetryToken token) => sequence.RetryWith(token);
    }
}
