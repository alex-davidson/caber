using System;
using System.Collections.Generic;
using System.Linq;

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
            try
            {
                // Start service.
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
                while (disposing.Any())
                {
                    DisposeTrackedComponent(disposing.Pop());
                }
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
                catch
                {
                }
            }
        }
    }
}
