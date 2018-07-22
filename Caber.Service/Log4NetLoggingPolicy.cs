using Caber.Logging;
using Caber.Service.Log4Net;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace Caber.Service
{
    public class Log4NetLoggingPolicy : ILoggingPolicy
    {
        public Log4NetConfiguration Configuration { get; } = new Log4NetConfiguration();

        public ILogging Initialise()
        {
            var hierarchy = new Hierarchy();
            var factory = new AppenderFactory(Configuration);

            hierarchy.Root.AddAppender(factory.CreateOperationsAppender());
            if (Configuration.EnableDiagnostics) hierarchy.Root.AddAppender(factory.CreateDiagnosticsAppender());
            hierarchy.Root.AddAppender(factory.CreateJsonAppender());

            var logger = hierarchy.GetLogger("LogEvents");
            return new Log4NetLogging(logger);
        }

        private class Log4NetLogging : ILogging, IOperationsLog, IDiagnosticsLog
        {
            private readonly ILogger logger;

            public Log4NetLogging(ILogger logger)
            {
                this.logger = logger;
            }

            IDiagnosticsLog ILogging.Diagnostics => this;
            IOperationsLog ILogging.Operations => this;
            void IDiagnosticsLog.Debug(LogEvent logEvent) => Write(Level.Debug, logEvent);
            void IDiagnosticsLog.Info(LogEvent logEvent)  => Write(Level.Info, logEvent);
            void IOperationsLog.Warn(LogEvent logEvent)   => Write(Level.Warn, logEvent);
            void IOperationsLog.Error(LogEvent logEvent)  => Write(Level.Error, logEvent);

            private void Write(Level level, LogEvent logEvent) => logger.Log(GetType(), level, logEvent, null);
        }
    }
}
