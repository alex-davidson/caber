using System;
using System.Security.Claims;

namespace Caber.Service.Http
{
    public interface ICaberRequestContext
    {
        ClaimsPrincipal GetPeerPrincipal();
        IServiceProvider Services { get; }

        void WriteResponseMessage<T>(int statusCode, T message);
        void NotFound();
    }
}
