using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Caber.FileSystem;
using Caber.Logging;
using Caber.Util;

namespace Caber.LocalState
{
    public class LocalFilesystemStore : ILocalStore
    {
        private readonly string rootPath;
        private readonly IStateSerialiserProvider serialiser;
        private readonly IDiagnosticsLog diagnosticsLog;

        public TimeSpan Timeout { get; set; }
        public IClock Clock { get; set; } = Util.Clock.Default;

        public LocalFilesystemStore(string rootPath, IStateSerialiserProvider serialiser, IDiagnosticsLog diagnosticsLog = null)
        {
            if (!PathUtils.IsNormalisedAbsolutePath(rootPath)) throw new ArgumentException($"Not a valid absolute path: {rootPath}", nameof(rootPath));

            this.rootPath = rootPath;
            this.serialiser = serialiser;
            this.diagnosticsLog = diagnosticsLog;
        }

        private AtomicFile<T> GetKeyFile<T>(string key)
        {
            if (!PathUtils.IsValidPathSegmentName(key)) throw new ArgumentException($"Key contains invalid filename characters: {key}");
            // Squash case: callers should never depend upon casing.
            var keyFilePath = Path.Combine(rootPath, key.ToUpper(), "value");
            return new AtomicFile<T>(keyFilePath, serialiser.Create<T>(), diagnosticsLog);
        }

        public T GetValue<T>(string key)
        {
            var op = new Deadline(Clock, Timeout);
            var file = GetKeyFile<T>(key);
            do
            {
                try
                {
                    return file.Read();
                }
                catch (Exception ex)
                {
                    op.OnError(ex);
                }
            }
            while (!op.HasExpired);
            throw RecordTimeout(op, key);
        }

        public void RemoveKey(string key)
        {
            var op = new Deadline(Clock, Timeout);
            var file = GetKeyFile<object>(key);
            do
            {
                try
                {
                    file.Delete();
                    return;
                }
                catch (Exception ex)
                {
                    op.OnError(ex);
                }
            }
            while (!op.HasExpired);
            throw RecordTimeout(op, key);
        }

        public void SetValue<T>(string key, T value)
        {
            var op = new Deadline(Clock, Timeout);
            var file = GetKeyFile<T>(key);
            do
            {
                try
                {
                    file.Write(value);
                    return;
                }
                catch (Exception ex)
                {
                    op.OnError(ex);
                }
            }
            while (!op.HasExpired);
            throw RecordTimeout(op, key);
        }

        private TimeoutException RecordTimeout(Deadline op, string key, [CallerMemberName] string operation = null)
        {
            var failure = op.Timeout();
            diagnosticsLog.Info(new LocalStoreTimeoutEvent(key, operation, failure));
            return failure;
        }
    }
}
