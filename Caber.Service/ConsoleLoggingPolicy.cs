using System;
using Caber.Logging;

namespace Caber.Service
{
    public class ConsoleLoggingPolicy : ILoggingPolicy
    {
        public bool EnableDiagnostics { get; set; } = true;
        public bool EnableDebugDiagnostics { get; set; } = false;

        public ILogging Initialise()
        {
            return new ConsoleLogging(this);
        }

        private class ConsoleLogging : ILogging, IOperationsLog, IDiagnosticsLog
        {
            private readonly ConsoleLoggingPolicy policy;

            public ConsoleLogging(ConsoleLoggingPolicy policy)
            {
                this.policy = policy;
            }

            IDiagnosticsLog ILogging.Diagnostics => this;
            IOperationsLog ILogging.Operations => this;

            void IDiagnosticsLog.Debug(LogEvent logEvent)
            {
                if (!policy.EnableDebugDiagnostics) return;
                if (!policy.EnableDiagnostics) return;
                Write("[DEBUG]", logEvent);
            }

            void IDiagnosticsLog.Info(LogEvent logEvent)
            {
                if (!policy.EnableDiagnostics) return;
                Write("[INFO] ", logEvent);
            }

            void IOperationsLog.Warn(LogEvent logEvent)   => Write("[WARN] ", logEvent);
            void IOperationsLog.Error(LogEvent logEvent)  => Write("[ERROR]", logEvent);

            private static void Write(string level, LogEvent logEvent) => Console.Error.WriteLine($"{level} {logEvent.Category}: {logEvent.FormatMessage()}");
        }
    }
}
