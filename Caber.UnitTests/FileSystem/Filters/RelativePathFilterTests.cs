using System;
using System.Text.RegularExpressions;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem.Filters
{
    [TestFixture]
    public class RelativePathFilterTests
    {
        private RelativePath SomeArbitraryPath => RelativePath.CreateFromSegments("some", "arbitrary", "path", Guid.NewGuid().ToString());

        [Test]
        public void DefaultFilter_IncludesAllPaths()
        {
            var filter = default(RelativePathFilter);

            var result = filter.Evaluate(SomeArbitraryPath);

            Assert.That(result.Rule, Is.EqualTo(FilterRule.Include));
        }

        [Test]
        public void ReturnsLastApplicableMatcher()
        {
            var expected = new RelativePathMatcher(new Regex("^some/arbitrary/path/.*$"), FilterRule.Include);
            Assume.That(expected.Matches(SomeArbitraryPath), Is.True);

            var matchers = new [] {
                default,
                new RelativePathMatcher(new Regex(@"^(.*/)?\.caber(/.*)?$"), FilterRule.Exclude),
                expected
            };
            var filter = new RelativePathFilter(matchers);

            var result = filter.Evaluate(SomeArbitraryPath);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
