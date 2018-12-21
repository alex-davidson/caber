using Microsoft.AspNetCore.Routing;

namespace Caber.Service.Http
{
    public class CaberRouteData
    {
        public ICaberRoute Route { get; set; }
        public RouteValueDictionary Parameters { get; set; }
    }
}
