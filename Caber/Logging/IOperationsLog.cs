namespace Caber.Logging
{
    public interface IOperationsLog
    {
        void Warn(LogEvent logEvent);
        void Error(LogEvent logEvent);
    }
}
