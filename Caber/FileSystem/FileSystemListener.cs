using System;
using System.IO;
using System.Threading;
using Caber.Logging;
using Caber.Routing;

namespace Caber.FileSystem
{
    public class FileSystemListener : IDisposable
    {
        private readonly LocalRoot root;
        private readonly FileSystemPathPool pool;
        private FileSystemWatcher watcher;
        private const int MaximumBufferSize = 256 * 1024;
        private readonly AutoResetEvent notified = new AutoResetEvent(false);
        /// <summary>
        /// Set when the listener processes an event. Intended for testing only.
        /// </summary>
        public WaitHandle Notified => notified;

        public FileSystemListener(LocalRoot root, FileSystemPathPool pool)
        {
            this.root = root;
            this.pool = pool;
            StartWatcher();
        }

        /// <summary>
        /// Flush pending events to the pool.
        /// </summary>
        /// <remarks>
        /// Currently a no-op as the FileSystemListener itself does not buffer events.
        /// </remarks>
        public void Flush()
        {
        }

        private void StartWatcher(int bufferSize = 8192)
        {
            try
            {
                DestroyWatcher();
                CreateWatcher(bufferSize);
            }
            catch (Exception ex)
            {
                Log.Operations.Error(new FileSystemWatcherErrorEvent(root.RootUri.LocalPath, ex));
            }
        }

        private void CreateWatcher(int bufferSize)
        {
            var newWatcher = new FileSystemWatcher(root.RootUri.LocalPath);
            newWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite;
            newWatcher.Created += Watcher_Created;
            newWatcher.Changed += Watcher_Changed;
            newWatcher.Renamed += Watcher_Renamed;
            newWatcher.Error += Watcher_Error;
            newWatcher.InternalBufferSize = bufferSize;
            newWatcher.IncludeSubdirectories = true;

            watcher = newWatcher;
            watcher.EnableRaisingEvents = true;
        }

        private void DestroyWatcher()
        {
            if (watcher == null) return;
            watcher.EnableRaisingEvents = false;
            watcher.Created -= Watcher_Created;
            watcher.Changed -= Watcher_Changed;
            watcher.Renamed -= Watcher_Renamed;
            watcher.Error -= Watcher_Error;
            watcher.Dispose();
            watcher = null;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e) => OnNotify(e.FullPath);
        private void Watcher_Changed(object sender, FileSystemEventArgs e) => OnNotify(e.FullPath);
        private void Watcher_Renamed(object sender, RenamedEventArgs e) => OnNotify(e.FullPath);

        private void OnNotify(string fullPath)
        {
            pool.Add(fullPath);
            notified.Set();
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            if (exception is InternalBufferOverflowException)
            {
                Log.Diagnostics.Info(new FileSystemWatcherBufferOverflowEvent(root.RootUri.LocalPath));
                notified.Set();
                TryEnlargeBuffer();
                return;
            }
            Log.Operations.Error(new FileSystemWatcherErrorEvent(root.RootUri.LocalPath, exception));
            notified.Set();
        }

        private void TryEnlargeBuffer()
        {
            var bufferSize = watcher.InternalBufferSize;
            var newBufferSize = Math.Min(bufferSize * 2, MaximumBufferSize);
            if (newBufferSize <= bufferSize) return;

            StartWatcher(newBufferSize);
        }

        public void Dispose()
        {
            DestroyWatcher();
        }

        private class FileSystemWatcherBufferOverflowEvent : LogEvent, ILogEventJsonDto
        {
            public FileSystemWatcherBufferOverflowEvent(string path)
            {
                this.Path = path;
            }

            public override LogEventCategory Category => LogEventCategory.Routing;
            public override string FormatMessage() => $"FileSystemWatcher for {Path} was unable to buffer all events. Some may be lost.";
            public override ILogEventJsonDto GetDtoForJson() => this;

            public string Path { get; }
        }

        private class FileSystemWatcherErrorEvent : LogEvent, ILogEventJsonDto
        {
            private readonly Exception exception;

            public FileSystemWatcherErrorEvent(string path, Exception exception)
            {
                this.Path = path;
                this.exception = exception;
            }

            public override LogEventCategory Category => LogEventCategory.Routing;
            public override string FormatMessage() => $"FileSystemWatcher for {Path}: {exception}";
            public override ILogEventJsonDto GetDtoForJson() => this;

            public string Path { get; }
            public ExceptionDto Exception => ExceptionDto.MapFrom(exception);
        }
    }
}
