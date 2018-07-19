using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Caber.FileSystem.Filters
{
    /// <summary>
    /// Generates a regex from a glob pattern and a filesystem casing rule.
    /// </summary>
    /// <remarks>
    /// The resulting regex will only match paths which use '/' as their directory
    /// separator. No other separator is supported. Paths should be normalised before
    /// being matched.
    /// The glob pattern may use the local platform's directory separator, in which
    /// case it will be silently normalised to '/' during regex construction.
    /// </remarks>
    public partial class GlobToRegexCompiler
    {
        private static char[] GetDirectorySeparatorsToReplace(params char[] knownSeparators) => knownSeparators.Except(new [] { '/' }).Distinct().ToArray();
        private static readonly char[] LocalPlatformExtraDirectorySeparators = GetDirectorySeparatorsToReplace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        private readonly char[] extraDirectorySeparators;

        public GlobToRegexCompiler()
        {
            extraDirectorySeparators = LocalPlatformExtraDirectorySeparators;
        }

        /// <summary>
        /// Unit testing constructor. Permits specifying which characters should be
        /// treated as directory separators when interpreting globs, instead of using
        /// the local platform's behaviour.
        /// </summary>
        /// <param name="directorySeparators"></param>
        internal GlobToRegexCompiler(char[] directorySeparators)
        {
            extraDirectorySeparators = GetDirectorySeparatorsToReplace(directorySeparators);
        }

        public Regex CompileRegex(string globPattern, FileSystemCasing casing)
        {
            var normalisedGlobPattern = SquashDirectorySeparators(globPattern);
            var regexPattern = new Internal().Compile(normalisedGlobPattern);
            return FileSystemRegexHelpers.CreateRegex(regexPattern, casing);
        }

        private string SquashDirectorySeparators(string globPattern)
        {
            foreach (var s in extraDirectorySeparators)
            {
                globPattern = globPattern.Replace(s, '/');
            }
            return globPattern;
        }
    }
}
