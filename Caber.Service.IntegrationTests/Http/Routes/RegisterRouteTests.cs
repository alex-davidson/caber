using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Caber.Authentication;
using Caber.Server;
using Caber.Service.Http.Routes;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Caber.Service.IntegrationTests.Http.Routes
{
    [TestFixture]
    public class RegisterRouteTests
    {
        [Test]
        public async Task MinimalValidRequestInvokesAction()
        {
            var validAuthentication = MockAuthentication.Get();
            var action = Mock.Of<RegisterAction>();
            using (var server = CreateServer(validAuthentication, action))
            {
                await server.StartAsync();

                var request = CreateMinimalValidRequest(validAuthentication);

                await server.MakeClientRequest(validAuthentication,
                    $"/register/{validAuthentication.ClientUuid}",
                    HttpMethod.Post,
                    new StringContent(JsonConvert.SerializeObject(request)));

                Mock.Get(action).Verify(a => a.Execute(It.IsAny<RegisterAction.Request>(), It.IsAny<ClaimsPrincipal>()));
            }
        }

        [Test]
        public async Task AcceptedResultIs200OK()
        {
            var validAuthentication = MockAuthentication.Get();
            var actionResponse = new RegisterAction.Response
            {
                Type = RegisterAction.ResponseType.Accepted,
                ServerIdentity = new CaberIdentity { Uuid = validAuthentication.ServerUuid.Value },
                AcceptedRoots = new string[0],
            };
            var action = Mock.Of<RegisterAction>(a => a.Execute(It.IsAny<RegisterAction.Request>(), It.IsAny<ClaimsPrincipal>()) == actionResponse);
            using (var server = CreateServer(validAuthentication, action))
            {
                await server.StartAsync();

                var request = CreateMinimalValidRequest(validAuthentication);

                var response = await server.MakeClientRequest(validAuthentication,
                    $"/register/{validAuthentication.ClientUuid}",
                    HttpMethod.Post,
                    new StringContent(JsonConvert.SerializeObject(request)));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task EnvironmentNotSupportedResultIs501NotSupported()
        {
            var validAuthentication = MockAuthentication.Get();
            var actionResponse = new RegisterAction.Response
            {
                Type = RegisterAction.ResponseType.EnvironmentNotSupported,
                ServerIdentity = new CaberIdentity { Uuid = validAuthentication.ServerUuid.Value },
                Environment = new CaberSharedEnvironment()
            };
            var action = Mock.Of<RegisterAction>(a => a.Execute(It.IsAny<RegisterAction.Request>(), It.IsAny<ClaimsPrincipal>()) == actionResponse);
            using (var server = CreateServer(validAuthentication, action))
            {
                await server.StartAsync();

                var request = CreateMinimalValidRequest(validAuthentication);

                var response = await server.MakeClientRequest(validAuthentication,
                    $"/register/{validAuthentication.ClientUuid}",
                    HttpMethod.Post,
                    new StringContent(JsonConvert.SerializeObject(request)));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotImplemented));
            }
        }

        [Test]
        public async Task UriClientUuidMismatchIs400BadRequest()
        {
            var validAuthentication = MockAuthentication.Get();
            var action = Mock.Of<RegisterAction>();
            using (var server = CreateServer(validAuthentication, action))
            {
                await server.StartAsync();

                var request = CreateMinimalValidRequest(validAuthentication);

                var response = await server.MakeClientRequest(validAuthentication,
                    $"/register/{Guid.NewGuid()}",
                    HttpMethod.Post,
                    new StringContent(JsonConvert.SerializeObject(request)));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task MessageClientUuidMismatchIs400BadRequest()
        {
            var validAuthentication = MockAuthentication.Get();
            var action = Mock.Of<RegisterAction>();
            using (var server = CreateServer(validAuthentication, action))
            {
                await server.StartAsync();

                var request = CreateMinimalValidRequest(validAuthentication);
                request.clientIdentity.uuid = Guid.NewGuid();

                var response = await server.MakeClientRequest(validAuthentication,
                    $"/register/{validAuthentication.ClientUuid}",
                    HttpMethod.Post,
                    new StringContent(JsonConvert.SerializeObject(request)));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        private static RegisterRoute.RequestDto CreateMinimalValidRequest(MockAuthentication validAuthentication)
        {
            return new RegisterRoute.RequestDto
            {
                clientIdentity = new RegisterRoute.CaberIdentityDto { uuid = validAuthentication.ClientUuid.Value },
                serverIdentity = new RegisterRoute.CaberUuidDto { uuid = validAuthentication.ServerUuid.Value },
                environment = new RegisterRoute.EnvironmentDto(),
                roots = new RegisterRoute.RootDto[0]
            };
        }

        private TestServer CreateServer(MockAuthentication authentication, RegisterAction action)
        {
            var server = new TestServer(
                new CaberServerBuilder
                {
                    ServerUrl = "https://localhost:0",
                    Authentication = authentication
                });
            server.AddService(action);
            return server;
        }
    }
}
