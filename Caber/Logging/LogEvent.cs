using System;

namespace Caber.Logging
{
    public abstract class LogEvent
    {
        public DateTimeOffset Timestamp { get; protected set; } = DateTimeOffset.Now;
        public abstract LogEventCategory Category { get; }
        public virtual string EventName => GetType().Name;
        /// <summary>
        /// Format the event as a human-readable message, without a trailing line break.
        /// </summary>
        public abstract string FormatMessage();
        /// <summary>
        /// Get a JSON-serialisable DTO representing the event. It is valid (and encouraged)
        /// for a LogEvent to be its own DTO.
        /// </summary>
        public abstract ILogEventJsonDto GetDtoForJson();
    }
}
