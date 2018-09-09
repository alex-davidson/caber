namespace Caber.LocalState
{
    public interface ILocalStore : IReadableLocalStore
    {
        void RemoveKey(string key);
        void SetValue<T>(string key, T value);
    }
}
