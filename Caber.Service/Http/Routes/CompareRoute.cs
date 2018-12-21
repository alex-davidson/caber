using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.Http.Routes
{
    public class CompareRoute : ICaberRoute
    {
        public RouteTemplate Template { get; } = TemplateParser.Parse("compare/{client-uuid}/{root-name}");
        public ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters)
        {
            throw new NotImplementedException();
        }
    }
}
