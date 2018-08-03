using System.Collections.Generic;

namespace Caber.Retry
{
    internal class RetryableSequence : IRetryCollector, IRetryParent
    {
        private readonly IRetryParent parent;
        private readonly int index;
        private RetryToken self;
        private List<RetryToken> children;

        public RetryableSequence(IRetryParent parent, int index)
        {
            this.parent = parent;
            this.index = index;
        }

        public IRetryCollector Parallel()
        {
            if (children == null) children = new List<RetryToken>();
            children.Add(default);
            return new RetryableSequence(this, children.Count - 1);
        }

        public void RetryWith(RetryToken token)
        {
            self &= token;
            parent.Notify(GetAggregate(), index);
        }

        void IRetryParent.Notify(RetryToken token, int childIndex)
        {
            children[childIndex] = token;
            parent.Notify(GetAggregate(), index);
        }

        private RetryToken GetAggregate()
        {
            if (children == null) return self;
            var token = self;
            foreach (var child in children)
            {
                token |= child;
            }
            return token;
        }
    }
}
