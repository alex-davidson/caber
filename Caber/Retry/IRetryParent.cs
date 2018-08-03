namespace Caber.Retry
{
    internal interface IRetryParent
    {
        void Notify(RetryToken token, int i);
    }
}
