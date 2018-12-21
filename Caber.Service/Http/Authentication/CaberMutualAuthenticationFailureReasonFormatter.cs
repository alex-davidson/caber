using System;

namespace Caber.Service.Http.Authentication
{
    public class CaberMutualAuthenticationFailureReasonFormatter
    {
        public string Format(CaberMutualAuthenticationFailureReason failure)
        {
            switch (failure)
            {
                case CaberMutualAuthenticationFailureReason.MissingOrInvalidClientUUID: return "Missing or invalid client UUID (" + CaberHeaders.SenderUuid + ")";
                case CaberMutualAuthenticationFailureReason.MissingOrInvalidServerUUID: return "Missing or invalid server UUID (" + CaberHeaders.RecipientUuid + ")";
                case CaberMutualAuthenticationFailureReason.ServerUUIDDoesNotReferToThisInstance: return "Server UUID does not refer to this instance (" + CaberHeaders.SenderUuid + ")";
                case CaberMutualAuthenticationFailureReason.NoClientCertificateProvided: return "No client certificate provided.";
                case CaberMutualAuthenticationFailureReason.ClientCertificateDoesNotMatchAnyKnownForTheClaimedUUID: return "Client certificate does not match any known for the claimed UUID.";
                default: throw new ArgumentOutOfRangeException(nameof(failure), failure, "Unrecognised CaberMutualAuthenticationFailureReason");
            }
        }
    }
}
