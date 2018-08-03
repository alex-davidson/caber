namespace Caber.Retry
{
    public interface IFaultTracker
    {
        bool TryProceed(out RetryToken token);

        RetryToken Fault();
        void Success();
    }
}
