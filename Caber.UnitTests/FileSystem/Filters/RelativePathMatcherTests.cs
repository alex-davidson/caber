using System;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem.Filters
{
    [TestFixture]
    public class RelativePathMatcherTests
    {
        [Test]
        public void DefaultMatcher_IncludesAllPaths()
        {
            var matcher = default(RelativePathMatcher);

            Assert.That(matcher.Matches(RelativePath.CreateFromSegments("some", "arbitrary", "path", Guid.NewGuid().ToString())), Is.True);
            Assert.That(matcher.Rule, Is.EqualTo(FilterRule.Include));
        }
    }
}
