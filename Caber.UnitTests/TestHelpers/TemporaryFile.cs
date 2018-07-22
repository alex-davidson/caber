using System;
using System.IO;

namespace Caber.UnitTests.TestHelpers
{
    public class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            if (File.Exists(path)) throw new ArgumentException($"File exists: {path}", nameof(path));
            FullPath = path;
        }

        public string FullPath { get; }

        public void CleanUp() => File.Delete(FullPath);

        public void Dispose()
        {
            CleanUp();
            GC.SuppressFinalize(this);
        }

        ~TemporaryFile()
        {
            try { CleanUp(); }
            catch { }
        }

        public static TemporaryFile CreateNew()
        {
            var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return new TemporaryFile(filePath);
        }
    }
}
