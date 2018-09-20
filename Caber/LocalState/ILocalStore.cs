using System;
using System.Diagnostics;
using System.IO;
using Caber.Logging;
using Caber.Util;

namespace Caber.LocalState
{
    public interface ILocalStore : IReadableLocalStore
    {
        void RemoveKey(string key);
        void SetValue<T>(string key, T value);
    }
}
