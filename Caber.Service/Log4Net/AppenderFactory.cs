using System;
using System.IO;
using Caber.Logging;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using Newtonsoft.Json;

namespace Caber.Service.Log4Net
{
    internal class AppenderFactory
    {
        private readonly Log4NetConfiguration configuration;
        private Level MinimumLogLevel =>
              !configuration.EnableDiagnostics      ? Level.Notice
            : !configuration.EnableDebugDiagnostics ? Level.Info
            : Level.All;

        public AppenderFactory(Log4NetConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IAppender CreateOperationsAppender() =>
            CreateDailyRollingAppender("Operations", "operations", new HumanReadableLayout(), new LevelRangeFilter { LevelMin = Level.Notice, LevelMax = Level.Off });

        public IAppender CreateDiagnosticsAppender() =>
            CreateDailyRollingAppender("Diagnostics", "diagnostics", new HumanReadableLayout(), new LevelRangeFilter { LevelMin = MinimumLogLevel, LevelMax = Level.Info });

        public IAppender CreateJsonAppender() =>
            CreateDailyRollingAppender("Json", "json", new MachineReadableLayout(), new LevelRangeFilter { LevelMin = MinimumLogLevel, LevelMax = Level.Off });

        private RollingFileAppender CreateDailyRollingAppender(string appenderName, string logName, ILayout layout, IFilter filter)
        {
            var path = Path.Combine(configuration.LogRootDirectory, logName, logName);
            var appender = new RollingFileAppender
            {
                Name = appenderName,
                File = path,
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Date,
                StaticLogFileName = false,
                DatePattern = @"'.'yyyy-MM-dd'.log'",
                LockingModel = new FileAppender.ExclusiveLock(),
                Layout = Init(layout)
            };
            appender.AddFilter(Init(filter));
            return Init(appender);
        }

        private class HumanReadableLayout : LayoutSkeleton
        {
            private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff K";

            public override void ActivateOptions() { }

            public override void Format(TextWriter writer, LoggingEvent loggingEvent)
            {
                var logEvent = GetLogEvent(loggingEvent);
                // Log local time, but include offset.
                writer.Write(logEvent.Timestamp.ToLocalTime().ToString(TimestampFormat));
                writer.Write(" - ");
                writer.WriteLine(logEvent.FormatMessage());
            }
        }

        private class MachineReadableLayout : LayoutSkeleton
        {
            public override string ContentType => "application/json";
            private readonly JsonSerializer jsonSerialiser = new JsonSerializer { Formatting = Formatting.None };

            public override void ActivateOptions() { }

            public override void Format(TextWriter writer, LoggingEvent loggingEvent)
            {
                var logEvent = GetLogEvent(loggingEvent);
                jsonSerialiser.Serialize(writer, logEvent.GetDtoForJson());
            }
        }

        private class UnknownEvent : LogEvent, ILogEventJsonDto
        {
            public UnknownEvent(LoggingEvent loggingEvent)
            {
                Timestamp = new DateTimeOffset(loggingEvent.TimeStampUtc, TimeSpan.Zero);
                Message = loggingEvent.RenderedMessage;
            }

            public override LogEventCategory Category => LogEventCategory.None;
            public override string FormatMessage() => Message;
            public override ILogEventJsonDto GetDtoForJson() => this;

            public string Message { get; }
        }

        private static LogEvent GetLogEvent(LoggingEvent loggingEvent)
        {
            if (loggingEvent.MessageObject is LogEvent logEvent) return logEvent;
            // If this event didn't come from our own logging interfaces, generate something vaguely sane.
            return new UnknownEvent(loggingEvent);
        }

        private static T Init<T>(T obj)
        {
            (obj as IOptionHandler)?.ActivateOptions();
            return obj;
        }
    }
}
