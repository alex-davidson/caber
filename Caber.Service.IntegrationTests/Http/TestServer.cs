using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Caber.Service.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Caber.Service.IntegrationTests.Http
{
    internal class TestServer : IDisposable
    {
        private readonly CaberServerBuilder builder;
        private readonly IWebHostBuilder kestrelBuilder;
        private IWebHost host;

        public TestServer(CaberServerBuilder builder)
        {
            this.builder = builder;
            kestrelBuilder = builder.CreateKestrelBuilder();
        }

        public void AddService<TService>(TService impl) where TService : class
        {
            if (host != null) throw new InvalidOperationException("Server already started.");

            kestrelBuilder.ConfigureServices(s => s.AddSingleton<TService>(impl));
        }

        public async Task StartAsync()
        {
            if (host != null) throw new InvalidOperationException("Server already started.");

            host = kestrelBuilder.Build();
            await host.StartAsync();

            var addresses = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
            var portNumber = new Uri(addresses.First()).Port;
            var uriBuilder = new UriBuilder(builder.ServerUrl) { Port = portNumber };
            RootUri = uriBuilder.Uri;
        }

        public Uri RootUri { get; private set; }

        public async Task<HttpResponseMessage> MakeClientRequest(MockAuthentication clientAuthentication, string relativeUri, HttpMethod method = null, HttpContent content = null)
        {
            using (var client = CreateClient(clientAuthentication))
            {
                return await client.SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri(RootUri, relativeUri),
                    Method = method ?? HttpMethod.Get,
                    Headers =
                    {
                        {CaberHeaders.RecipientUuid, clientAuthentication.ServerUuid.ToString()}
                    },
                    Content = content
                });
            }
        }

        private static HttpClient CreateClient(MockAuthentication authentication)
        {
            var clientHandler = new HttpClientHandler();
            if (authentication.ClientCertificate != null) clientHandler.ClientCertificates.Add(authentication.ClientCertificate);
            clientHandler.ServerCertificateCustomValidationCallback =
                (request, certificate, chain, policyErrors) => authentication.ValidateServerPeerCertificate(default, certificate, chain, policyErrors);
            clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;

            var client = new HttpClient(clientHandler);
            if (authentication.ClientUuid != null) client.DefaultRequestHeaders.Add(CaberHeaders.SenderUuid, authentication.ClientUuid.ToString());
            return client;
        }

        public void Dispose()
        {
            host?.Dispose();
        }
    }
}
