using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Caber.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Caber.Service.Http.Authentication
{
    public class CaberMutualAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "CaberMutual";
        public const string SchemeDescription = "Caber Mutual Authentication";

        public const string FailureReasonEntry = "CaberMutualAuthenticationHandler_FailureReason";

        public ICaberMutualAuthentication Authentication { get; }

        public CaberMutualAuthenticationHandler(ICaberMutualAuthentication authentication, IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            Authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        }

        private AuthenticateResult Fail(CaberMutualAuthenticationFailureReason failure)
        {
            var properties = new AuthenticationProperties();
            properties.SetParameter(FailureReasonEntry, failure);
            return AuthenticateResult.Fail(new CaberMutualAuthenticationFailureReasonFormatter().Format(failure), properties);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!CaberHttpUtil.ReadUuid(Context.Request.Headers, CaberHeaders.SenderUuid, out var senderUuid)) return Fail(CaberMutualAuthenticationFailureReason.MissingOrInvalidClientUUID);
            if (!CaberHttpUtil.ReadUuid(Context.Request.Headers, CaberHeaders.RecipientUuid, out var recipientUuid)) return Fail(CaberMutualAuthenticationFailureReason.MissingOrInvalidServerUUID);

            if (recipientUuid != Authentication.GetOwnIdentity().Uuid)
            {
                return Fail(CaberMutualAuthenticationFailureReason.ServerUUIDDoesNotReferToThisInstance);
            }

            var clientCertificate = await Context.Connection.GetClientCertificateAsync();
            if (clientCertificate == null) return Fail(CaberMutualAuthenticationFailureReason.NoClientCertificateProvided);

            var result = Authentication.ValidatePeerIdentity(clientCertificate, senderUuid);
            switch (result)
            {
                case PeerIdentityValidationResult.Mismatch:
                    return Fail(CaberMutualAuthenticationFailureReason.ClientCertificateDoesNotMatchAnyKnownForTheClaimedUUID);

                case PeerIdentityValidationResult.NotOnRecord:
                    return AuthenticateResult.NoResult();

                case PeerIdentityValidationResult.OK:
                    return AuthenticateResult.Success(
                        new AuthenticationTicket(
                            CaberPrincipal.CreateClaimsPrincipal(SchemeName, senderUuid, clientCertificate.Thumbprint),
                            SchemeName));

                default: throw new ArgumentOutOfRangeException(nameof(result), result, "Unrecognised PeerIdentityValidationResult");
            }
        }
    }
}
