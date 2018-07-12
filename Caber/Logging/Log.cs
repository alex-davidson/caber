namespace Caber.Logging
{
    public static class Log
    {
        public static void Configure(ILoggingPolicy loggingPolicy)
        {
            logging = loggingPolicy.Initialise();
        }

        private static ILogging logging = new NotConfigured();

        public static IDiagnosticsLog Diagnostics => logging.Diagnostics;
        public static IOperationsLog Operations => logging.Operations;

        private class NotConfigured : ILogging, IOperationsLog, IDiagnosticsLog
        {
            IDiagnosticsLog ILogging.Diagnostics => this;
            IOperationsLog ILogging.Operations => this;
            void IDiagnosticsLog.Debug(LogEvent logEvent) { }
            void IDiagnosticsLog.Info(LogEvent logEvent) { }
            void IOperationsLog.Warn(LogEvent logEvent) { }
            void IOperationsLog.Error(LogEvent logEvent) { }
        }
    }
}
