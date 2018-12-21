using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.Http.Routes
{
    public class AppendRoute : ICaberRoute
    {
        public RouteTemplate Template { get; } = TemplateParser.Parse("append/{client-uuid}/{root-name}/{*path}");
        public ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters)
        {
            throw new NotImplementedException();
        }
    }
}
