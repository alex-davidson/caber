using System;

namespace Caber.Logging
{
    public interface ILogEventJsonDto
    {
        DateTimeOffset Timestamp { get; }
        LogEventCategory Category { get; }
        string EventName { get; }
    }
}
