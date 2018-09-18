namespace Caber.LocalState
{
    public interface IReadableLocalStore
    {
        T GetValue<T>(string key);
    }
}
