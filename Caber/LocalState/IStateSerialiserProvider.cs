namespace Caber.LocalState
{
    public interface IStateSerialiserProvider
    {
        IStateSerialiser<T> Create<T>();
    }
}
