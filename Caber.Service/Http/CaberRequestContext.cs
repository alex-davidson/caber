using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Caber.Service.Http
{
    public class CaberRequestContext : ICaberRequestContext
    {
        public JsonProvider JsonProvider { get; set; } = new JsonProvider();
        public HttpContext HttpContext { get; set; }
        public CaberRouteData RouteData { get; set; }

        public ClaimsPrincipal GetPeerPrincipal() => HttpContext?.User;
        public IServiceProvider Services => HttpContext.RequestServices;

        public virtual void WriteResponseMessage<T>(int statusCode, T message)
        {
            HttpContext.Response.StatusCode = statusCode;
            HttpContext.Response.ContentType = "application/json";
            JsonProvider.Serialise(HttpContext.Response.Body, message);
        }

        public virtual void NotFound()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}
