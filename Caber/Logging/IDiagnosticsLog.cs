namespace Caber.Logging
{
    public interface IDiagnosticsLog
    {
        void Debug(LogEvent logEvent);
        void Info(LogEvent logEvent);
    }
}
