namespace Caber.LocalState
{
    public interface IServiceState
    {
        ILocalStore Observed { get; }
        IReadableLocalStore Specified { get; }
        ILocalStore Provisional { get; }
    }
}
