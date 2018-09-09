using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Caber.FileSystem;

namespace Caber.Authorisation
{
    public struct PeerIdentity
    {
        public string Name { get; set; }
        public string ShortCode { get; set; }
        public string CertificateThumbprint { get; set; }
    }

    public interface IAuthorisationService
    {
        void TryReceive(AbstractPath path, PeerIdentity identity);
    }

    public interface IAuthorisationAdministrationService
    {
    }

    public class AuthorisationService
    {
    }

    public abstract class AuthorisationException : SecurityException
    {
    }

    public class PeerIdentityService
    {
        public void ValidateIdentity(PeerIdentity identity, CertificateThumbprint thumbprint)
        {
            /*
1. Validate the peer's certificate, ie. don't give away anything until they
   prove that they have the private key for the certificate they present.
   This should be done by our HTTP library.
2. Look up the name/shortcode in the Specified section.
   * If not present, fail.
   * If present but disabled, fail.
3. Look up the thumbprint in the Provisional section.
   * If present but name and shortcode don't match, fail.
4. Look up the UUID in the Observed section. If present:
   * If the name and shortcode don't match, fail.
   * If the thumbprint doesn't match any in-date *and* the thumbprint wasn't
     in the Provisional section, fail.
     */
        }
    }
}
