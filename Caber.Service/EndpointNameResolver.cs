using System;
using System.Linq;
using System.Net;

namespace Caber.Service
{
    internal class EndpointNameResolver
    {
        public IPEndPoint[] ResolveToIPEndPoints(string url)
        {
            var uri = new Uri(url);
            return ResolveHostNameToIPAddresses(uri.DnsSafeHost)
                .Select(x => new IPEndPoint(x, uri.Port))
                .ToArray();
        }

        public IPAddress[] ResolveHostNameToIPAddresses(string host)
        {
            if (IPAddress.TryParse(host, out var ip)) return new [] { ip };

            var dnsEntry = Dns.GetHostEntry(host);
            if (!dnsEntry.AddressList.Any()) throw new InvalidOperationException($"Hostname did not resolve to any addresses: {host}");
            return dnsEntry.AddressList.Where(x => !x.IsIPv6LinkLocal).ToArray();
        }
    }
}
