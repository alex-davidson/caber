using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Caber.Routing
{
    public class FileSystemPathPool
    {
        private readonly HashSet<string> set = new HashSet<string>();
        private readonly AutoResetEvent queued = new AutoResetEvent(false);
        public WaitHandle Queued => queued;

        public void Add(ISet<string> changes)
        {
            if (!changes.Any()) return;
            lock (set)
            {
                set.UnionWith(changes);
            }
            queued.Set();
        }

        public void Add(string change)
        {
            lock (set)
            {
                if (!set.Add(change)) return;
            }
            queued.Set();
        }

        public bool TryTake(out string path)
        {
            path = default;
            lock (set)
            {
                using (var iterator = set.GetEnumerator())
                {
                    if (!iterator.MoveNext()) return false;
                    path = iterator.Current;
                    iterator.Dispose();
                    set.Remove(path);
                    return true;
                }
            }
        }
    }
}
