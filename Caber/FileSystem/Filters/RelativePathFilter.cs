using System;
using System.Collections.Generic;
using System.Linq;

namespace Caber.FileSystem.Filters
{
    /// <summary>
    /// Encapsulates inclusion and exclusion rules for RelativePaths.
    /// </summary>
    public struct RelativePathFilter
    {
        private readonly RelativePathMatcher[] matchers;

        public RelativePathFilter(IEnumerable<RelativePathMatcher> matchers) : this()
        {
            if (matchers == null) throw new ArgumentNullException(nameof(matchers));
            this.matchers = PrepareMatchers(matchers);
        }

        private static RelativePathMatcher[] PrepareMatchers(IEnumerable<RelativePathMatcher> matchers)
        {
            // Reverse the list ahead of time to so we can find the first match instead
            // of the last.
            var matchFirstOrder = matchers.Reverse().ToArray();
            if (matchFirstOrder.Length == 0) return null;   // Default filter, essentially.
            return matchFirstOrder;
        }

        /// <summary>
        /// Returns the last matcher which applies to the specified path.
        /// </summary>
        public RelativePathMatcher Evaluate(RelativePath relativePath)
        {
            if (matchers == null) return default;   // default(RelativePathFilter) should include all paths.
            for (var i = 0; i < matchers.Length; i++)
            {
                if (matchers[i].Matches(relativePath)) return matchers[i];
            }
            // Fallthrough: the default behaviour is 'include all'.
            return default;
        }

        public bool Exists => matchers != null;
    }
}
