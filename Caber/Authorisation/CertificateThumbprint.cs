using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Caber.Authorisation
{
    public class CertificateThumbprint : IEquatable<CertificateThumbprint>
    {
        private readonly byte[] bytes;
        private readonly string formatted;

        private CertificateThumbprint(byte[] bytes, string formatted)
        {
            this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            this.formatted = formatted;
            if (bytes.Length < 4) throw new ArgumentException("SANITY CHECK: Thumbprint contains fewer than 4 bytes!");
        }

        public static CertificateThumbprint FromCertificate(X509Certificate cert)
        {
            if (cert == null) throw new ArgumentNullException(nameof(cert));
            return new CertificateThumbprint(cert.GetCertHash(), cert.GetCertHashString());
        }

        public bool Equals(CertificateThumbprint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return bytes.SequenceEqual(other.bytes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CertificateThumbprint)obj);
        }

        public override int GetHashCode() => (bytes[3] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[0];

        public override string ToString() => formatted;
    }
}
