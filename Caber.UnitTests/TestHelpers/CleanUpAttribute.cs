using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Caber.UnitTests.TestHelpers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CleanUpAttribute : Attribute, ITestAction
    {
        public void BeforeTest(ITest test) => TestDisposables.Begin(test.Id);
        public void AfterTest(ITest test) => TestDisposables.End(test.Id);
        public ActionTargets Targets => ActionTargets.Test;
    }

    public static class CleanUpExtensions
    {
        public static void DisposeAfterTest(this TestContext context, IDisposable obj)
        {
            TestDisposables.For(context.Test.ID).Add(obj);
        }
    }
}
