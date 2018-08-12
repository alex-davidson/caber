using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Caber.Service.Http
{
    public class CaberRequestRouter : ICaberRequestRouter
    {
        private readonly List<ICaberRoute> routes = new List<ICaberRoute>();

        public CaberRequestRouter Add<T>() where T : ICaberRoute, new()
        {
            routes.Add(new T());
            return this;
        }

        public CaberRouteData Route(CaberRequestContext context)
        {
            foreach (var route in routes)
            {
                var routeData = TryRoute(route, context);
                if (routeData != null) return routeData;
            }
            return null;
        }

        private CaberRouteData TryRoute(ICaberRoute route, CaberRequestContext context)
        {
            var routeContext = new RouteContext(context.HttpContext);
            var matcher = new TemplateMatcher(route.Template, new RouteValueDictionary());
            var routeParameters = new RouteValueDictionary();
            if (matcher.TryMatch(routeContext.HttpContext.Request.Path, routeParameters))
            {
                return new CaberRouteData { Route = route, Parameters = routeParameters };
            }
            return null;
        }
    }
}
