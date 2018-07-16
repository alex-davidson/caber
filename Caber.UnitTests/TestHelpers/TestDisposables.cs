using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Caber.UnitTests.TestHelpers
{
    public class TestDisposables
    {
        private static readonly ConcurrentDictionary<string, TestDisposables> DisposablesPerTest = new ConcurrentDictionary<string, TestDisposables>();

        public static void Begin(string testId)
        {
            if (DisposablesPerTest.TryAdd(testId, new TestDisposables())) return;
            throw new InvalidOperationException("Test context already has a list of disposables. BUG?");
        }

        public static TestDisposables For(string testId)
        {
            if (DisposablesPerTest.TryGetValue(testId, out var list)) return list;
            throw new InvalidOperationException("Test context has no list of disposables. Missing [CleanUp]?");
        }

        public static void End(string testId)
        {
            if (DisposablesPerTest.TryRemove(testId, out var disposables)) disposables.Clean();
        }

        private readonly Stack<IDisposable> disposables = new Stack<IDisposable>();

        public void Add(IDisposable disposable)
        {
            disposables.Push(disposable);
        }

        public void Clean()
        {
            var cleanList = disposables.ToArray();
            disposables.Clear();
            foreach (var item in cleanList) item.Dispose();
        }
    }
}
