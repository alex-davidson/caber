using System.IO;
using System.Security.Cryptography;

namespace Caber.Util
{
    public class Sha256Hasher
    {
        public Hash Hash(Stream stream)
        {
            using (var impl = SHA256.Create())
            {
                var bytes = impl.ComputeHash(stream);
                return new Hash(bytes);
            }
        }
    }
}
