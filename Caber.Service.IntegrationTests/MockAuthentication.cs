using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Caber.Authentication;
using Caber.Cryptography;

namespace Caber.Service.IntegrationTests
{
    public class MockAuthentication : ICaberMutualAuthentication
    {
        private static readonly CertificateFactory TestFactory = new CertificateFactory{ RsaKeySizeBits = 512, Asn1SignatureAlgorithm = "SHA256WITHRSA" };
        public static MockAuthentication Get() => CachedInstance.Copy();

        /// <summary>
        /// Creating certificates is slow, so create them once and just reuse.
        /// </summary>
        private static readonly MockAuthentication CachedInstance = new MockAuthentication();

        private MockAuthentication()
        {
            ServerCertificate = TestFactory.CreateMutualAuthenticationX509($"uid={ServerUuid:D},dc=caber", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
            ClientCertificate = TestFactory.CreateMutualAuthenticationX509($"uid={ClientUuid:D},dc=caber", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        }

        public MockAuthentication Copy() => new MockAuthentication
        {
            ServerUuid = ServerUuid,
            ServerCertificate = ServerCertificate,
            ServerCertificateIsValid = ServerCertificateIsValid,
            ClientUuid = ClientUuid,
            ClientCertificate = ClientCertificate,
            ClientCertificateIsValid = ClientCertificateIsValid,
            ValidatePeerIdentityResult = ValidatePeerIdentityResult
        };

        public Guid? ServerUuid { get; set; } = Guid.NewGuid();
        public X509Certificate2 ServerCertificate { get; set; }
        public bool ServerCertificateIsValid { get; set; } = true;

        public Guid? ClientUuid { get; set; } = Guid.NewGuid();
        public X509Certificate2 ClientCertificate { get; set; }
        public bool ClientCertificateIsValid { get; set; } = true;

        public PeerIdentityValidationResult ValidatePeerIdentityResult { get; set; } = PeerIdentityValidationResult.OK;

        public X509Certificate2 GetCurrentCertificate() => ServerCertificate;
        public CaberIdentity GetOwnIdentity() => new CaberIdentity { Uuid = ServerUuid.Value };
        public bool ValidateClientPeerCertificate(X509Certificate2 peerCertificate, X509Chain x509Chain, SslPolicyErrors policyErrors) => peerCertificate.Thumbprint == ClientCertificate.Thumbprint && ClientCertificateIsValid;
        public bool ValidateServerPeerCertificate(Guid peerUuid, X509Certificate2 peerCertificate, X509Chain x509Chain, SslPolicyErrors policyErrors) => peerCertificate.Thumbprint == ServerCertificate.Thumbprint && ServerCertificateIsValid;
        public PeerIdentityValidationResult ValidatePeerIdentity(X509Certificate2 peerCertificate, Guid peerIdentityUuid) => ValidatePeerIdentityResult;
    }
}
