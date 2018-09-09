using System.IO;

namespace Caber.FileSystem
{
    public class FileLockedException : IOException
    {
        public FileLockedException(string path, IOException original) : base($"File is locked by another process: {path}", original)
        {
        }

        public static bool Matches(IOException exception) => (uint)exception.HResult == 0x80070020;
    }
}
