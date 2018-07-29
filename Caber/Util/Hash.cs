using System;

namespace Caber.Util
{
    /// <summary>
    /// Default-comparable representation of a hash.
    /// </summary>
    public struct Hash
    {
        public static Hash None => default;

        private readonly string hexString;

        public Hash(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            hexString = bytes.ToHexString();
        }

        public override string ToString() => hexString;
        public byte[] GetBytes() => ByteArrayExtensions.FromHexString(hexString);
    }
}
