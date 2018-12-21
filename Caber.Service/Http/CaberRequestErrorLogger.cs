using System;
using Caber.Logging;

namespace Caber.Service.Http
{
    public class CaberRequestErrorLogger
    {
        public void BadRequest(Exception exception, Type routeType)
        {
            Log.Diagnostics.Debug(new HttpBadRequestEvent(exception, routeType));
        }

        private class HttpBadRequestEvent : LogEvent, ILogEventJsonDto
        {
            private readonly Exception exception;
            private readonly Type routeType;

            public HttpBadRequestEvent(Exception exception, Type routeType)
            {
                this.exception = exception;
                this.routeType = routeType;
            }

            public override string FormatMessage() => $"Bad Request: {exception}";

            public TypeDto RouteType => TypeDto.MapFrom(routeType);
            public ExceptionDto Exception => ExceptionDto.MapFrom(exception);
            public override LogEventCategory Category => LogEventCategory.None;
            public override ILogEventJsonDto GetDtoForJson() => this;
        }
    }
}
