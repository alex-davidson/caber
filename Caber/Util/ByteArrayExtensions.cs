using System;
using System.Text;

namespace Caber.Util
{
    public static class ByteArrayExtensions
    {
        private static readonly string HexDigits =  "0123456789abcdef";

        public static string ToHexString(this byte[] bytes)
        {
            var buffer = new StringBuilder(bytes.Length);
            foreach (var b in bytes)
            {
                var high = b >> 4;
                var low = b & 0x0f;
                buffer.Append(HexDigits[high]);
                buffer.Append(HexDigits[low]);
            }
            return buffer.ToString();
        }

        public static byte[] FromHexString(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}
