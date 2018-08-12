using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.Http.Routes
{
    public class RegisterRoute : ICaberRoute
    {
        public RouteTemplate Template { get; } = TemplateParser.Parse("register/{client-uuid}");
        public ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters)
        {
            throw new NotImplementedException();
        }
    }
}
