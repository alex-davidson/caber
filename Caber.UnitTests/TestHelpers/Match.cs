using System;
using System.Linq.Expressions;
using Moq;

namespace Caber.UnitTests.TestHelpers
{
    internal static class Match
    {
        /// <summary>
        /// Generate an IEquatable which tests the specified expression.
        /// </summary>
        public static IEquatable<T> Object<T>(Expression<Func<T, bool>> match) => Mock.Of<IEquatable<T>>(e => e.Equals(It.Is(match)));
    }
}
