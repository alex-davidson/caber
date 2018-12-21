using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Caber.Authentication;
using Caber.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;

namespace Caber.Service.Http.Routes
{
    public class RegisterRoute : ICaberRoute
    {
        public RouteTemplate Template { get; } = TemplateParser.Parse("register/{client-uuid}");
        public ICaberRouteHandler Create(IServiceProvider services, RouteValueDictionary parameters)
        {
            return new Handler(parameters["client-uuid"].ToString());
        }

        private class Handler : ICaberRouteHandler
        {
            private readonly string clientUuid;

            public Handler(string clientUuid)
            {
                this.clientUuid = clientUuid;
            }

            public Task ExecuteAsync(CaberRequestContext context)
            {
                var action = context.Services.GetService<RegisterAction>();
                var requestDto = new RequestDto();
                try
                {
                    context.ReadRequestMessage(requestDto);
                    ValidateRequest(requestDto, context.GetPeerPrincipal());
                }
                catch (Exception ex)
                {
                    context.Logger.BadRequest(ex, typeof(RegisterRoute));
                    context.BadRequest();
                    return Task.CompletedTask;
                }

                var request = new RegisterAction.Request
                {
                    ClientIdentity = new CaberIdentity { Uuid = requestDto.clientIdentity.uuid },
                    ServerIdentity = new CaberIdentity { Uuid = requestDto.serverIdentity.uuid },
                    Environment = new CaberSharedEnvironment
                    {
                        HashAlgorithm = requestDto.environment.hashAlgorithm
                    },
                    Roots = requestDto.roots.Select(r => r.name).ToArray()
                };

                var response = action.Execute(request, context.GetPeerPrincipal());

                switch (response.Type)
                {
                    case RegisterAction.ResponseType.Accepted:
                        context.WriteResponseMessage(StatusCodes.Status200OK, new SucceededResponseDto
                        {
                            serverIdentity = new CaberIdentityDto { uuid = response.ServerIdentity.Uuid },
                            acceptedRoots = response.AcceptedRoots.Select(r => new RootDto { name = r }).ToArray()
                        });
                        break;

                    case RegisterAction.ResponseType.EnvironmentNotSupported:
                        context.WriteResponseMessage(StatusCodes.Status501NotImplemented, new NotImplementedResponseDto
                        {
                            serverIdentity = new CaberIdentityDto { uuid = response.ServerIdentity.Uuid },
                            environment = new EnvironmentDto { hashAlgorithm = response.Environment.HashAlgorithm },
                        });
                        break;

                    default:
                        context.BugCheck();
                        break;
                }

                return Task.CompletedTask;
            }

            private void ValidateRequest(RequestDto requestDto, ClaimsPrincipal peerPrincipal)
            {
                if (requestDto.clientIdentity == null) throw new ArgumentException($"{nameof(requestDto.clientIdentity)} must be provided.");
                if (requestDto.serverIdentity == null) throw new ArgumentException($"{nameof(requestDto.serverIdentity)} must be provided.");
                if (requestDto.environment == null) throw new ArgumentException($"{nameof(requestDto.environment)} must be provided.");
                if (requestDto.roots == null) throw new ArgumentException($"{nameof(requestDto.roots)} must be provided.");

                if (requestDto.clientIdentity.uuid != Guid.Parse(clientUuid)) throw new ArgumentException("UUID in URI does not match UUID in message.");
                if (requestDto.clientIdentity.uuid != peerPrincipal.GetClaimedUuid()) throw new ArgumentException("UUID of client principal does not match UUID in message.");
            }
        }

        public class RequestDto
        {
            public CaberIdentityDto clientIdentity { get; set; }
            public CaberUuidDto serverIdentity { get; set; }
            public EnvironmentDto environment { get; set; }
            public RootDto[] roots { get; set; }
        }

        public class SucceededResponseDto
        {
            public CaberIdentityDto serverIdentity { get; set; }
            public RootDto[] acceptedRoots { get; set; }
        }

        public class NotImplementedResponseDto
        {
            public CaberIdentityDto serverIdentity { get; set; }
            public EnvironmentDto environment { get; set; }
        }

        public class RootDto
        {
            public string name { get; set; }
        }

        public class EnvironmentDto
        {
            public string hashAlgorithm { get; set; }
        }

        public class CaberIdentityDto
        {
            public Guid uuid { get; set; }
            public string name { get; set; }
            public string code { get; set; }
        }

        public class CaberUuidDto
        {
            public Guid uuid { get; set; }
        }
    }
}
