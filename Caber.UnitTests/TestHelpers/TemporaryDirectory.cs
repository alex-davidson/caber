using System;
using System.IO;

namespace Caber.UnitTests.TestHelpers
{
    public class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            if (Directory.Exists(path)) throw new ArgumentException($"Directory exists: {path}", nameof(path));
            FullPath = path;
        }

        public string FullPath { get; }

        public string File(string fileName) => Path.Combine(FullPath, fileName);

        public void CleanUp() => Directory.Delete(FullPath, true);

        public void Dispose()
        {
            CleanUp();
            GC.SuppressFinalize(this);
        }

        ~TemporaryDirectory()
        {
            try { CleanUp(); }
            catch { }
        }

        public static TemporaryDirectory CreateNew()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return new TemporaryDirectory(directoryPath);
        }
    }
}
