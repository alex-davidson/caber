using System;

namespace Caber.Logging
{
    /// <summary>
    /// Represents an exception which we don't yet know how to handle. Every
    /// occurrence of this exception in the logs should be considered a bug.
    /// Cases should either be properly handled, or a specific diagnostic or
    /// operational event logged instead.
    /// </summary>
    public class UnexpectedExceptionEvent : LogEvent, ILogEventJsonDto
    {
        private readonly Exception exception;

        public UnexpectedExceptionEvent(LogEventCategory category, Exception exception, string context)
        {
            this.exception = exception;
            Category = category;
            Context = context;
        }

        public override LogEventCategory Category { get; }
        public override string FormatMessage() => $"Unexpected exception: {exception}\nContext: {Context}";
        public override ILogEventJsonDto GetDtoForJson() => this;

        public ExceptionDto Exception => ExceptionDto.MapFrom(exception);
        public string Context { get; }
    }
}
