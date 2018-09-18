using System;
using Caber.Logging;

namespace Caber.LocalState
{
    public class LocalStoreCorruptFileStreamEvent : LogEvent, ILogEventJsonDto
    {
        private readonly Exception exception;

        public LocalStoreCorruptFileStreamEvent(string path, FormatException exception)
        {
            this.exception = exception;
            this.Path = path;
        }

        public override LogEventCategory Category => LogEventCategory.None;
        public override ILogEventJsonDto GetDtoForJson() => this;

        public override string FormatMessage() => $"File '{Path}' could not be read: {Exception}";

        public string Path { get; }
        public ExceptionDto Exception => ExceptionDto.MapFrom(exception);
    }
}
