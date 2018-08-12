using System;
using System.Threading.Tasks;
using Caber.Service.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.IntegrationTests.Http
{
    internal class StatusCodeRoute : ICaberRoute
    {
        private readonly int statusCode;

        public StatusCodeRoute(int statusCode)
        {
            this.statusCode = statusCode;
        }

        public RouteTemplate Template => null;
        public ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters) => new Handler(statusCode);

        private class Handler : ICaberRouteHandler
        {
            private readonly int statusCode;

            public Handler(int statusCode)
            {
                this.statusCode = statusCode;
            }

            public Task ExecuteAsync(CaberRequestContext context)
            {
                context.WriteResponseMessage(statusCode, new object());
                return Task.CompletedTask;
            }
        }
    }
}
