using System;
using Caber.Logging;

namespace Caber.LocalState
{
    public class LocalStoreTimeoutEvent : LogEvent, ILogEventJsonDto
    {
        private readonly Exception exception;

        public LocalStoreTimeoutEvent(string key, string operation, TimeoutException exception)
        {
            this.exception = exception.InnerException;
            Key = key;
            Operation = operation;
        }

        public override LogEventCategory Category => LogEventCategory.None;
        public override ILogEventJsonDto GetDtoForJson() => this;

        public override string FormatMessage() => exception == null
            ? $"Timed out during {Operation} on key '{Key}'"
            : $"Timed out during {Operation} on key '{Key}': {exception}";

        public string Key { get; }
        public string Operation { get; }
        public ExceptionDto Exception => ExceptionDto.MapFrom(exception);
    }
}
