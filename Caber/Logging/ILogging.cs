namespace Caber.Logging
{
    public interface ILogging
    {
        IDiagnosticsLog Diagnostics { get; }
        IOperationsLog Operations { get; }
    }
}
