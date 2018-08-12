using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace Caber.Cryptography
{
    public class CertificateFactory
    {
        public int RsaKeySizeBits { get; set; } = 2048;
        public string Asn1SignatureAlgorithm { get; set; } = "SHA512WITHRSA";

        public X509Certificate2 CreateMutualAuthenticationX509(string fullSubject, DateTimeOffset validFrom, DateTimeOffset expires)
        {
            var random = GetSecureRandom();

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage.Id, true,
                new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth, KeyPurposeID.IdKPClientAuth));

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Issuer and Subject Name
            var subjectDN = new X509Name(fullSubject);
            var subjectKeyPair = GenerateRsaKeyPair();

            var issuerDN = subjectDN;
            var issuerKeyPair = subjectKeyPair;

            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            certificateGenerator.SetNotBefore(validFrom.DateTime);
            certificateGenerator.SetNotAfter(expires.DateTime);
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var signatureFactory = new Asn1SignatureFactory(Asn1SignatureAlgorithm, issuerKeyPair.Private, random);
            var certificate = certificateGenerator.Generate(signatureFactory);

            var store = new Pkcs12Store();
            var friendlyName = certificate.SubjectDN.ToString();
            var certificateEntry = new X509CertificateEntry(certificate);
            var keyEntry = new AsymmetricKeyEntry(subjectKeyPair.Private);

            store.SetCertificateEntry(friendlyName, certificateEntry);
            store.SetKeyEntry(friendlyName, keyEntry, new[] { certificateEntry });
            var stream = new MemoryStream();
            store.Save(stream, new char[0], random);

            var bytes = stream.ToArray();
            return new X509Certificate2(bytes, (string)null, X509KeyStorageFlags.PersistKeySet);
        }

        private static SecureRandom GetSecureRandom() => new SecureRandom(new CryptoApiRandomGenerator());

        private AsymmetricCipherKeyPair GenerateRsaKeyPair()
        {
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(GetSecureRandom(), RsaKeySizeBits));
            return keyPairGenerator.GenerateKeyPair();
        }

        public X509Certificate2 CreateMutualAuthenticationX509(X500DistinguishedName fullSubject, DateTimeOffset validFrom, DateTimeOffset expires)
        {
            return CreateMutualAuthenticationX509(fullSubject.Format(false), validFrom, expires);
        }
    }
}
