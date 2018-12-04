using System;
using Caber.Authentication;
using Caber.Service.Http;
using Caber.Service.Http.Authentication;
using Caber.Service.Http.Routes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;

namespace Caber.Service
{
    public class CaberServerBuilder
    {
        public string ServerUrl { get; set; }
        public ICaberMutualAuthentication Authentication { get; set; }
        public ICaberRequestRouter RequestRouter { get; set; } = new CaberRequestRouter()
            .Add<RegisterRoute>()
            .Add<CompareRoute>()
            .Add<WriteRoute>()
            .Add<AppendRoute>();

        private void Validate()
        {
            if (string.IsNullOrEmpty(ServerUrl)) throw new InvalidOperationException("ServerUrl must be specified.");
            if (Authentication == null) throw new InvalidOperationException("Authentication must be specified.");
            if (RequestRouter == null) throw new InvalidOperationException("RequestRouter must be specified.");
        }

        public IWebHostBuilder CreateKestrelBuilder()
        {
            Validate();
            var webHostBuilder = new WebHostBuilder()
                .UseKestrel(kestrel => {
                    kestrel.Limits.MaxRequestBodySize = null;

                    var adapterOptions = new HttpsConnectionAdapterOptions {
                        ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                        ClientCertificateValidation = Authentication.ValidateClientPeerCertificate,
                        ServerCertificateSelector = (c, n) => Authentication.GetCurrentCertificate(),
                        CheckCertificateRevocation = false,
                    };

                    foreach (var ip in new EndpointNameResolver().ResolveToIPEndPoints(ServerUrl))
                    {
                        kestrel.Listen(ip, x => x.UseHttps(adapterOptions));
                    }
                })
                .ConfigureServices(services => {
                    services.AddSingleton(typeof(ICaberMutualAuthentication), Authentication);
                    services.AddSingleton(RequestRouter);
                    services.AddSingleton<ICaberRequestHandler, CaberRequestHandler>();
                    services.AddTransient<CaberRequestErrorLogger>();

                    services.AddAuthentication(x => x.DefaultScheme = CaberMutualAuthenticationHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, CaberMutualAuthenticationHandler>(CaberMutualAuthenticationHandler.SchemeName, CaberMutualAuthenticationHandler.SchemeDescription, o => { });
                })
                .UseStartup<Startup>();

            return webHostBuilder;
        }

        private class Startup : IStartup
        {
            private readonly ICaberRequestRouter router;
            private readonly ICaberRequestHandler handler;

            public Startup(ICaberRequestRouter router, ICaberRequestHandler handler)
            {
                this.router = router;
                this.handler = handler;
            }

            public IServiceProvider ConfigureServices(IServiceCollection services) => services.BuildServiceProvider();

            public void Configure(IApplicationBuilder app)
            {
                app.UseAuthentication();
                app.Use(_ => async context => {
                    var requestContext = new CaberRequestContext { HttpContext = context };
                    requestContext.RouteData = router.Route(requestContext);
                    await handler.HandleAsync(requestContext);
                });
            }
        }
    }
}
