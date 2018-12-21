using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Caber.Authentication;
using Caber.Service.Http;
using Caber.Service.Http.Authentication;
using Microsoft.AspNetCore.Routing;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Caber.Service.IntegrationTests.Http
{
    [TestFixture]
    public class AuthenticationTests
    {
        [Test]
        public async Task RequestWithCertificatesAndHeaders_Yields200OK()
        {
            var validAuthentication = MockAuthentication.Get();
            using (var server = CreateServer(validAuthentication))
            {
                await server.StartAsync();

                var response = await server.MakeClientRequest(validAuthentication, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task MissingXCaberRecipientHeader_Yields401Unauthorized()
        {
            var validAuthentication = MockAuthentication.Get();
            using (var server = CreateServer(validAuthentication))
            {
                await server.StartAsync();

                var invalid = validAuthentication.Copy();
                invalid.ServerUuid = null;

                var response = await server.MakeClientRequest(invalid, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                var message = JsonConvert.DeserializeObject<CaberAuthenticationFailureMessage>(await response.Content.ReadAsStringAsync());
                Assert.That(message.Reason, Is.EqualTo(CaberMutualAuthenticationFailureReason.MissingOrInvalidServerUUID.ToString()));
            }
        }

        [Test]
        public async Task MissingXCaberSenderHeader_Yields401Unauthorized()
        {
            var validAuthentication = MockAuthentication.Get();
            using (var server = CreateServer(validAuthentication))
            {
                await server.StartAsync();

                var invalid = validAuthentication.Copy();
                invalid.ClientUuid = null;

                var response = await server.MakeClientRequest(invalid, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                var message = JsonConvert.DeserializeObject<CaberAuthenticationFailureMessage>(await response.Content.ReadAsStringAsync());
                Assert.That(message.Reason, Is.EqualTo(CaberMutualAuthenticationFailureReason.MissingOrInvalidClientUUID.ToString()));
            }
        }

        [Test]
        public async Task MissingClientCertificate_Yields401Unauthorized()
        {
            var validAuthentication = MockAuthentication.Get();
            using (var server = CreateServer(validAuthentication))
            {
                await server.StartAsync();

                var invalid = validAuthentication.Copy();
                invalid.ClientCertificate = null;

                var response = await server.MakeClientRequest(invalid, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                var message = JsonConvert.DeserializeObject<CaberAuthenticationFailureMessage>(await response.Content.ReadAsStringAsync());
                Assert.That(message.Reason, Is.EqualTo(CaberMutualAuthenticationFailureReason.NoClientCertificateProvided.ToString()));
            }
        }

        [Test]
        public async Task IncorrectXCaberRecipientHeader_Yields401Unauthorized()
        {
            var validAuthentication = MockAuthentication.Get();
            using (var server = CreateServer(validAuthentication))
            {
                await server.StartAsync();

                var invalid = validAuthentication.Copy();
                invalid.ServerUuid = Guid.NewGuid();

                var response = await server.MakeClientRequest(invalid, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                var message = JsonConvert.DeserializeObject<CaberAuthenticationFailureMessage>(await response.Content.ReadAsStringAsync());
                Assert.That(message.Reason, Is.EqualTo(CaberMutualAuthenticationFailureReason.ServerUUIDDoesNotReferToThisInstance.ToString()));
            }
        }

        [Test]
        public async Task MismatchedClientUuidAndCertificate_Yields401Unauthorized()
        {
            var authentication = MockAuthentication.Get();
            authentication.ValidatePeerIdentityResult = PeerIdentityValidationResult.Mismatch;

            using (var server = CreateServer(authentication))
            {
                await server.StartAsync();

                var response = await server.MakeClientRequest(authentication, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                var message = JsonConvert.DeserializeObject<CaberAuthenticationFailureMessage>(await response.Content.ReadAsStringAsync());
                Assert.That(message.Reason, Is.EqualTo(CaberMutualAuthenticationFailureReason.ClientCertificateDoesNotMatchAnyKnownForTheClaimedUUID.ToString()));
            }
        }

        [Test]
        public async Task NotKnownClientUuidAndCertificate_Yields200OK()
        {
            var authentication = MockAuthentication.Get();
            authentication.ValidatePeerIdentityResult = PeerIdentityValidationResult.NotOnRecord;

            using (var server = CreateServer(authentication))
            {
                await server.StartAsync();

                var response = await server.MakeClientRequest(authentication, "/Test/Default");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task InvalidClientCertificate_YieldsErrorBeforeRouting()
        {
            var authentication = MockAuthentication.Get();
            authentication.ClientCertificateIsValid = false;

            var router = Mock.Of<ICaberRequestRouter>();

            using (var server = CreateServer(authentication, router))
            {
                await server.StartAsync();

                var exception = Assert.CatchAsync<HttpRequestException>(() => server.MakeClientRequest(authentication, "/Test/Default"));
                Assert.That(exception, Has.InnerException);
                Assert.That(exception, Has.InnerException.InnerException);
                Assert.That(exception, Has.InnerException.InnerException.Message.Contains("decryption"));
                Mock.Get(router).Verify(r => r.Route(It.IsAny<CaberRequestContext>()), Times.Never);
            }
        }

        [Test]
        public async Task InvalidServerCertificate_YieldsClientError()
        {
            var authentication = MockAuthentication.Get();
            authentication.ServerCertificateIsValid = false;

            var router = Mock.Of<ICaberRequestRouter>();

            using (var server = CreateServer(authentication, router))
            {
                await server.StartAsync();

                var exception = Assert.CatchAsync<HttpRequestException>(() => server.MakeClientRequest(authentication, "/Test/Default"));
                Assert.That(exception.InnerException?.InnerException, Is.InstanceOf<AuthenticationException>());
                Assert.That(exception.InnerException?.InnerException?.Message, Does.Contain("remote certificate is invalid"));
                Mock.Get(router).Verify(r => r.Route(It.IsAny<CaberRequestContext>()), Times.Never);
            }
        }

        private TestServer CreateServer(MockAuthentication authentication, ICaberRequestRouter router = null) =>
            new TestServer(
                new CaberServerBuilder {
                    ServerUrl = "https://localhost:0",
                    Authentication = authentication,
                    RequestRouter = router ?? Mock.Of<ICaberRequestRouter>(r =>
                        r.Route(It.IsAny<CaberRequestContext>()) == new CaberRouteData { Route = new StatusCodeRoute(200), Parameters = new RouteValueDictionary() } )
                });
    }
}
