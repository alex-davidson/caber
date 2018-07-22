using System;
using System.Linq;

namespace Caber.Logging
{
    public class ExceptionDto
    {
        public string Message { get; set; }
        public TypeDto Type { get; set; }
        public string AssemblyQualifiedTypeName { get; set; }
        public string StackTrace { get; set; }
        public ExceptionDto[] InnerExceptions { get; set; }

        public static ExceptionDto MapFrom(Exception exception)
        {
            if (exception == null) return null;
            return new ExceptionDto {
                Message = exception.Message,
                Type = TypeDto.MapFrom(exception.GetType()),
                AssemblyQualifiedTypeName = exception.GetType().AssemblyQualifiedName,
                StackTrace = exception.StackTrace,
                InnerExceptions = MapInnerExceptionsFrom(exception)
            };
        }

        private static ExceptionDto[] MapInnerExceptionsFrom(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                return aggregateException.InnerExceptions.Select(MapFrom).ToArray();
            }
            return new [] { MapFrom(exception.InnerException) };
        }
    }
}
