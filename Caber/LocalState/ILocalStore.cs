using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Caber.Logging;
using Caber.Util;

namespace Caber.LocalState
{
    public interface ILocalStore
    {
        T GetValue<T>(string key);
        void RemoveKey(string key);
        void SetValue<T>(string key, T value);
    }

    public interface IKeyValueStore
    {
        Task<T> GetValue<T>(string key, CancellationToken token = default);
        Task RemoveKey(string key, CancellationToken token = default);
        Task SetValue<T>(string key, T value, CancellationToken token = default);
    }

    public class FailureThrottle<T>
    {
        private readonly ConcurrentDictionary<T, Entry> failures = new ConcurrentDictionary<T, Entry>();
        private readonly int logThreshold;
        private readonly TimeSpan logRate;

        public FailureThrottle(int logThreshold, TimeSpan logRate)
        {
            this.logThreshold = logThreshold;
            this.logRate = logRate;
        }

        public IClock Clock { get; set; } = Util.Clock.Default;

        public bool Record(T key, Exception exception, out int failureCount)
        {
            failureCount = default;
            var entry = failures.GetOrAdd(key, new Entry());
            lock (entry)
            {
                entry.Exceptions.Enqueue(exception);
                if (entry.Exceptions.Count > 10) entry.Exceptions.Dequeue();
                entry.TimesRecorded++;
                entry.TimesRecordedSinceThreshold++;

                if (entry.TimesRecordedSinceThreshold < logThreshold) return false;
                if (entry.LastLogged + logRate <= Clock.Now) return false;

                failureCount = entry.TimesRecorded;
                entry.TimesRecordedSinceThreshold = 0;
                entry.LastLogged = Clock.Now;
                return true;
            }
        }

        public void Reset(T key)
        {
            failures.TryRemove(key, out _);
        }

        private class Entry
        {
            public Queue<Exception> Exceptions { get; }  = new Queue<Exception>();
            public DateTimeOffset LastLogged { get; set; } = DateTimeOffset.MinValue;
            public int TimesRecordedSinceThreshold { get; set; } = 0;
            public int TimesRecorded { get; set; } = 0;
        }
    }

    public class RetryingKeyValueStore : IKeyValueStore
    {
        private readonly ILocalStore localStore;
        private readonly IOperationsLog log;

        public RetryingKeyValueStore(ILocalStore localStore, IOperationsLog log = null)
        {
            this.localStore = localStore;
            this.log = log;
        }

        public Task<T> GetValue<T>(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task RemoveKey(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task SetValue<T>(string key, T value, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
