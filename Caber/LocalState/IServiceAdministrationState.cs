namespace Caber.LocalState
{
    public interface IServiceAdministrationState
    {
        ILocalStore Observed { get; }
        ILocalStore Specified { get; }
        ILocalStore Provisional { get; }
    }
}
