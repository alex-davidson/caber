using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Caber.Authentication
{
    public interface ICaberMutualAuthentication
    {
        /// <summary>
        /// Get this instance's current certificate.
        /// </summary>
        X509Certificate2 GetCurrentCertificate();

        /// <summary>
        /// Get the identity of this instance.
        /// </summary>
        /// <returns></returns>
        CaberIdentity GetOwnIdentity();

        /// <summary>
        /// Validate the peer's certificate prior to request processing.
        /// </summary>
        bool ValidateClientPeerCertificate(X509Certificate2 peerCertificate, X509Chain x509Chain, SslPolicyErrors policyErrors);
        bool ValidateServerPeerCertificate(Guid peerUuid, X509Certificate2 peerCertificate, X509Chain x509Chain, SslPolicyErrors policyErrors);

        /// <summary>
        /// Validate that the UUID provided by the peer matches what we have on record for its certificate.
        /// </summary>
        PeerIdentityValidationResult ValidatePeerIdentity(X509Certificate2 peerCertificate, Guid peerIdentityUuid);
    }
}
