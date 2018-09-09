using System;
using System.IO;
using Caber.FileSystem;
using Caber.Logging;

namespace Caber.LocalState
{
    public class LocalFilesystemStore : ILocalStore
    {
        private readonly string rootPath;
        private readonly IStateSerialiserProvider serialiser;
        private readonly IDiagnosticsLog diagnosticsLog;

        public LocalFilesystemStore(string rootPath, IStateSerialiserProvider serialiser, IDiagnosticsLog diagnosticsLog = null)
        {
            if (!PathUtils.IsNormalisedAbsolutePath(rootPath)) throw new ArgumentException($"Not a valid absolute path: {rootPath}", nameof(rootPath));

            this.rootPath = rootPath;
            this.serialiser = serialiser;
            this.diagnosticsLog = diagnosticsLog;
        }

        private AtomicFile<T> GetKeyFile<T>(string key)
        {
            var keyPath = Path.Combine(rootPath, key);
            Directory.CreateDirectory(keyPath);
            var keyFilePath = Path.Combine(keyPath, "value");
            return new AtomicFile<T>(keyFilePath, serialiser.Create<T>(), diagnosticsLog);
        }

        public T GetValue<T>(string key) => GetKeyFile<T>(key).Read();
        public void RemoveKey(string key) => GetKeyFile<object>(key).Delete();
        public void SetValue<T>(string key, T value) => GetKeyFile<T>(key).Write(value);
    }
}
