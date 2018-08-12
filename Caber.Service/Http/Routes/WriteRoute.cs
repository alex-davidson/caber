using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.Http.Routes
{
    public class WriteRoute : ICaberRoute
    {
        public RouteTemplate Template { get; } = TemplateParser.Parse("write/{client-uuid}/{root-name}/{*path}");
        public ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters)
        {
            throw new NotImplementedException();
        }
    }
}
