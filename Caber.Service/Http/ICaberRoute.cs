using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.Http
{
    public interface ICaberRoute
    {
        RouteTemplate Template { get; }
        ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters);
    }
}
