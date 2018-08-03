namespace Caber.Retry
{
    public interface IRetryCollector
    {
        IRetryCollector Parallel();
        void RetryWith(RetryToken token);
    }
}
