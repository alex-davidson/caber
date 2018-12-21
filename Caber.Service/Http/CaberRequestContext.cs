using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Caber.Service.Http
{
    public class CaberRequestContext : ICaberRequestContext
    {
        public JsonProvider JsonProvider { get; set; } = new JsonProvider();
        public HttpContext HttpContext { get; set; }
        public CaberRouteData RouteData { get; set; }

        public ClaimsPrincipal GetPeerPrincipal() => HttpContext?.User;
        public IServiceProvider Services => HttpContext.RequestServices;

        public virtual void ReadRequestMessage<T>(T dto)
        {
            JsonProvider.Deserialise(HttpContext.Request.Body, dto);
        }

        public virtual void WriteResponseMessage<T>(int statusCode, T message)
        {
            HttpContext.Response.StatusCode = statusCode;
            HttpContext.Response.ContentType = "application/json";
            JsonProvider.Serialise(HttpContext.Response.Body, message);
        }

        public virtual CaberRequestErrorLogger Logger => Services.GetService<CaberRequestErrorLogger>();

        public virtual void BadRequest()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        public virtual void NotFound()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        public virtual void BugCheck()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
