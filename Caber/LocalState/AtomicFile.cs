using System;
using System.IO;
using Caber.FileSystem;
using Caber.Logging;

namespace Caber.LocalState
{
    /// <summary>
    /// Atomically read, write and delete a file.
    /// </summary>
    /// <remarks>
    /// All operations use a lockfile to enforce mutual exclusion.
    /// In addition to this, writes keep the data file around as a backup until
    /// the new file is safely written.
    /// </remarks>
    public class AtomicFile<T>
    {
        private readonly string path;
        private readonly IStateSerialiser<T> serialiser;
        private readonly IDiagnosticsLog diagnosticsLog;
        private readonly string backupPath;
        private readonly string lockPath;
        private readonly string directoryPath;

        internal LowLevel LowLevelImpl { get; set; } = new LowLevel();

        public AtomicFile(string path, IStateSerialiser<T> serialiser, IDiagnosticsLog diagnosticsLog = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!PathUtils.IsNormalisedAbsolutePath(path)) throw new ArgumentException($"Not a valid absolute path: {path}", nameof(path));
            this.path = path;
            this.serialiser = serialiser;
            this.diagnosticsLog = diagnosticsLog;
            backupPath = path + ".backup";
            lockPath = path + ".lock";
            directoryPath = Path.GetDirectoryName(lockPath);
        }

        private IDisposable AcquireLock()
        {
            LowLevelImpl.CreateDirectory(directoryPath);
            return LowLevelImpl.AcquireLockFile(lockPath);
        }

        private bool TryRead(string readPath, out T value)
        {
            value = default;
            using (var stream = LowLevelImpl.OpenForSharedRead(readPath))
            {
                if (stream == null) return false;
                value = serialiser.Read(stream);
            }
            return true;
        }

        /// <summary>
        /// Read a value from the file.
        /// </summary>
        /// <remarks>
        /// The process is:
        /// * Try the fast path first:
        ///   * Read from the backup file if it exists (a write is in progress or failed).
        ///   * Otherwise, read from the data file.
        ///   * If neither existed, return empty value.
        /// * If the fast path threw an exception, take the slow path:
        /// * Lock.
        /// * Check for a backup file.
        ///   * If it exists, a previous write did not complete.
        ///   * Read from the backup file. If the read succeeds, return.
        ///   * If the file is corrupt, log an INFO event and delete the file.
        ///   * Otherwise bail out loudly (eg. sharing conflict, security permissions).
        /// * Read from the data file. If the read succeeds, return.
        /// * If the file is corrupt, log a DEBUG event and delete the file.
        /// * Otherwise bail out loudly (eg. sharing conflict, security permissions).
        /// * Finally, always unlock.
        /// </remarks>
        public T Read()
        {
            try
            {
                if (TryRead(backupPath, out var value)) return value;
                if (TryRead(path, out value)) return value;
                return default;
            }
            catch
            {
                using (AcquireLock())
                {
                    try
                    {
                        if (TryRead(backupPath, out var value)) return value;
                    }
                    catch (FormatException ex)
                    {
                        diagnosticsLog.Info(new LocalStoreCorruptFileStreamEvent(backupPath, ex));
                        LowLevelImpl.DeleteFile(backupPath);
                    }
                    try
                    {
                        if (TryRead(path, out var value))
                        {
                            CleanUpBackup();
                            return value;
                        }
                    }
                    catch (FormatException ex)
                    {
                        diagnosticsLog.Debug(new LocalStoreCorruptFileStreamEvent(path, ex));
                        LowLevelImpl.DeleteFile(path);
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// Write a value into the file.
        /// </summary>
        /// <remarks>
        /// The process is:
        /// * Lock.
        /// * Check for a backup file.
        ///   * If it exists, a previous write did not complete and we should keep this backup.
        ///   * Otherwise, rename data file to backup file.
        /// * Delete data file, if still present.
        /// * Write new data file and synchronously flush to disk.
        /// * Delete backup file.
        /// * Finally, always unlock.
        /// </remarks>
        public void Write(T value)
        {
            using (AcquireLock())
            {
                if (LowLevelImpl.FileExists(path))
                {
                    if (LowLevelImpl.FileExists(backupPath))
                    {
                        // A previous write did not complete. Data file may be corrupt.
                        // Keep this backup and remove the data file.
                        LowLevelImpl.DeleteFile(path);
                    }
                    else
                    {
                        // Backup the existing data file.
                        LowLevelImpl.MoveFile(path, backupPath);
                    }
                }
                using (var stream = LowLevelImpl.OpenForExclusiveWrite(path))
                {
                    serialiser.Write(stream, value);
                    stream.Flush(true);
                }
                LowLevelImpl.DeleteFile(backupPath);
            }
        }

        /// <summary>
        /// Delete the file and any backup.
        /// </summary>
        public void Delete()
        {
            using (AcquireLock())
            {
                LowLevelImpl.DeleteFile(path);
                LowLevelImpl.DeleteFile(backupPath);
            }
        }

        /// <summary>
        /// Remove the backup file, ignoring failures due to locking.
        /// </summary>
        private void CleanUpBackup()
        {
            try
            {
                LowLevelImpl.DeleteFile(backupPath);
            }
            catch (FileLockedException)
            {
                // Ignore
            }
        }

        internal class LowLevel
        {
            public virtual void CreateDirectory(string path) => Directory.CreateDirectory(path);
            public virtual bool FileExists(string path) => File.Exists(path);
            public virtual void MoveFile(string fromPath, string toPath) => File.Move(fromPath, toPath);

            public virtual void DeleteFile(string path)
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException ex) when (FileLockedException.Matches(ex))
                {
                    throw new FileLockedException(path, ex);
                }
            }

            public virtual IDisposable AcquireLockFile(string lockPath)
            {
                try
                {
                    return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
                }
                catch (IOException ex) when (FileLockedException.Matches(ex))
                {
                    throw new FileLockedException(lockPath, ex);
                }
            }

            public virtual Stream OpenForSharedRead(string readPath)
            {
                try
                {
                    if (!File.Exists(readPath)) return null;
                    return new FileStream(readPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (IOException ex) when (FileLockedException.Matches(ex))
                {
                    throw new FileLockedException(readPath, ex);
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            }

            public virtual FileStream OpenForExclusiveWrite(string writePath)
            {
                try
                {
                    return new FileStream(writePath, FileMode.Create, FileAccess.Write, FileShare.None);
                }
                catch (IOException ex) when (FileLockedException.Matches(ex))
                {
                    throw new FileLockedException(writePath, ex);
                }
            }
        }
    }
}
