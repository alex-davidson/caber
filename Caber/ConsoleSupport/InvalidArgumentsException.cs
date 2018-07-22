using System;

namespace Caber.ConsoleSupport
{
    public class InvalidArgumentsException : ApplicationException
    {
        public InvalidArgumentsException(string message) : base(message)
        {
        }
    }
}
