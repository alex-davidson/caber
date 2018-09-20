using System;
using System.Threading.Tasks;

namespace Caber.Util
{
    public class Deadline
    {
        private readonly Task deadlineTask;
        private Exception lastException;

        public Deadline(IClock clock, TimeSpan limit) : this(clock.WaitAsync(limit))
        {
        }

        public Deadline(Task deadlineTask)
        {
            this.deadlineTask = deadlineTask;
        }

        public void OnError(Exception exception)
        {
            lastException = exception;
        }

        public bool HasExpired => deadlineTask.IsCompleted;

        public TimeoutException Timeout()
        {
            if (lastException != null) return new TimeoutException("Operation exceeded the deadline with at least one failure.", lastException);
            return new TimeoutException("Operation exceeded the deadline.");
        }
    }
}
