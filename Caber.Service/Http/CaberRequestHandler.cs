using System.Threading.Tasks;
using Caber.Service.Http.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Caber.Service.Http
{
    public class CaberRequestHandler : ICaberRequestHandler
    {
        public async Task HandleAsync(CaberRequestContext context)
        {
            if (!await Authenticate(context)) return;

            if (context.RouteData != null)
            {
                var handler = context.RouteData.Route.Create(context.Services, context.RouteData.Parameters);
                await handler.ExecuteAsync(context);
            }
            else
            {
                context.NotFound();
            }
        }

        private async Task<bool> Authenticate(CaberRequestContext context)
        {
            var result = await context.HttpContext.AuthenticateAsync();
            if (result.Succeeded) return true;
            if (result.None &&  context.RouteData != null) return true;     // Let the route decide how to handle absent authentication.

            var failureCode = result.Properties.GetParameter<CaberMutualAuthenticationFailureReason>(CaberMutualAuthenticationHandler.FailureReasonEntry);
            context.WriteResponseMessage(
                StatusCodes.Status401Unauthorized,
                new CaberAuthenticationFailureMessage
                {
                    Message = result.Failure.Message,
                    Reason = failureCode == CaberMutualAuthenticationFailureReason.None ? null : failureCode.ToString()
                });
            return false;
        }
    }
}
