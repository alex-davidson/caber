using System;
using System.Collections.Generic;
using System.Linq;
using Caber.Logging;

namespace Caber.Service
{
    public class Service
    {
        /// <summary>
        /// Create all components of the service and return an object which may be
        /// disposed to shut it down.
        /// </summary>
        public IDisposable StartInstance()
        {
            var instance = new Instance();
            Log.Diagnostics.Info(new ServiceStartingEvent());
            try
            {
                // Start service.

                Log.Diagnostics.Info(new ServiceStartedEvent());
                return instance;
            }
            catch
            {
                instance.Dispose();
                throw;
            }
        }

        private class Instance : IDisposable
        {
            private Stack<object> components = new Stack<object>();

            /// <summary>
            /// Track a component of the service, holding a live reference to it until
            /// shutdown. It will be disposed at shutdown if necessary.
            /// </summary>
            public T Track<T>(T component) where T : class
            {
                components.Push(component);
                return component;
            }

            public void Dispose()
            {
                var disposing = components;
                components = null;
                if (disposing == null) return;
                Log.Diagnostics.Info(new ServiceStoppingEvent());
                while (disposing.Any())
                {
                    DisposeTrackedComponent(disposing.Pop());
                }
                Log.Diagnostics.Info(new ServiceStoppedEvent());
            }

            private static void DisposeTrackedComponent(object component)
            {
                try
                {
                    if (component is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception exception)
                {
                    Log.Diagnostics.Debug(new ServiceShutdownExceptionEvent(exception, component.GetType()));
                }
            }
        }

        private abstract class ServiceLifecycleEvent : LogEvent, ILogEventJsonDto
        {
            public override LogEventCategory Category => LogEventCategory.Lifecycle;
            public override ILogEventJsonDto GetDtoForJson() => this;
        }

        private class ServiceStartingEvent : ServiceLifecycleEvent
        {
            public override string FormatMessage() => "Service is starting...";
        }

        private class ServiceStartedEvent : ServiceLifecycleEvent
        {
            public override string FormatMessage() => "Service has started.";
        }

        private class ServiceStoppingEvent : ServiceLifecycleEvent
        {
            public override string FormatMessage() => "Service is stopping...";
        }

        private class ServiceStoppedEvent : ServiceLifecycleEvent
        {
            public override string FormatMessage() => "Service has stopped.";
        }

        private class ServiceShutdownExceptionEvent : ServiceLifecycleEvent
        {
            private readonly Exception exception;
            private readonly Type componentType;

            public ServiceShutdownExceptionEvent(Exception exception, Type componentType)
            {
                this.exception = exception;
                this.componentType = componentType;
            }

            public override string FormatMessage() => $"Exception occurred during shutdown: {exception}";

            public TypeDto ComponentType => TypeDto.MapFrom(componentType);
            public ExceptionDto Exception => ExceptionDto.MapFrom(exception);
        }
    }
}
