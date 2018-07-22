using System.Text.RegularExpressions;

namespace Caber.FileSystem.Filters
{
    internal static class FileSystemRegexHelpers
    {
        public static Regex CreateRegex(string regex, FileSystemCasing casing)
        {
            var options = RegexOptions.Compiled;
            if (casing == FileSystemCasing.CasePreservingInsensitive) options |= RegexOptions.IgnoreCase;
            return new Regex(regex, options);
        }
    }
}
