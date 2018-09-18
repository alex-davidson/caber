using System.IO;

namespace Caber.LocalState
{
    public interface IStateSerialiser<T>
    {
        void Write(Stream stream, T value);
        /// <summary>
        /// Attempt to read an object of type T from the stream.
        /// </summary>
        T Read(Stream stream);
    }
}
