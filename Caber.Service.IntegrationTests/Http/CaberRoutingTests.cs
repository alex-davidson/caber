using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caber.Service.Http;
using Caber.Service.Http.Routes;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Caber.Service.IntegrationTests.Http
{
    [TestFixture]
    public class CaberRoutingTests
    {
        private readonly ICaberRequestRouter router = new CaberServerBuilder().RequestRouter;
        private readonly Guid clientId = Guid.NewGuid();

        [Test]
        public void MatchesRegisterRoute()
        {
            var httpContext = new DefaultHttpContext
            {
                Request = { Path = $"/register/{clientId}" }
            };
            var requestContext = new CaberRequestContext {HttpContext = httpContext};
            var routeData = router.Route(requestContext);

            Assert.That(routeData.Route, Is.InstanceOf<RegisterRoute>());
            Assert.That(routeData.Parameters, Does.Contain(RouteValue("client-uuid", clientId)));
        }

        private KeyValuePair<string, object> RouteValue(string key, object value) =>
            new KeyValuePair<string, object>(key, value.ToString());
    }
}
