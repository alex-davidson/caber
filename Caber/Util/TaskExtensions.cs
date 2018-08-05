using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caber.Util
{
    public static class TaskExtensions
    {
        public static Task WaitOneAsync(this WaitHandle waitHandle, CancellationToken token)
        {
            if (waitHandle == null) throw new ArgumentNullException(nameof(waitHandle));
            var tcs = new TaskCompletionSource<bool>();
            token.Register(() => tcs.TrySetCanceled(token));
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle, (s, e) => tcs.TrySetResult(true), null, -1, true);
            // ReSharper disable once MethodSupportsCancellation
            tcs.Task.ContinueWith(_ => rwh.Unregister(null));
            return  tcs.Task;
        }

        public static Task AsTask(this CancellationToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            var tcs = new TaskCompletionSource<bool>();
            token.Register(() => tcs.TrySetCanceled(token));
            return  tcs.Task;
        }
    }
}
