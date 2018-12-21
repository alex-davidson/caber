using System;
using System.Linq;
using System.Security.Claims;

namespace Caber.Authentication
{
    public static class CaberPrincipal
    {
        public static ClaimsPrincipal CreateClaimsPrincipal(string authenticationType, Guid uuid, string certificateThumbprint)
        {
            var claims = new [] {
                new Claim(CaberClaimTypes.Uuid, uuid.ToString()),
                new Claim(CaberClaimTypes.X509Thumbprint, certificateThumbprint),
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
        }

        private static string GetClaimValue(ClaimsPrincipal principal, string type)
        {
            return principal.Claims.FirstOrDefault(c => c.Type == type)?.Value;
        }

        public static Guid? GetClaimedUuid(this ClaimsPrincipal principal)
        {
            var value = GetClaimValue(principal, CaberClaimTypes.Uuid);
            if (value != null && Guid.TryParse(value, out var uuid)) return uuid;
            return null;
        }

        public static string GetClaimedX509Thumbprint(this ClaimsPrincipal principal)
        {
            return GetClaimValue(principal, CaberClaimTypes.X509Thumbprint);
        }
    }
}
