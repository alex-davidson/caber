using System.Text.RegularExpressions;

namespace Caber.FileSystem.Filters
{
    public class FileExtensionToRegexCompiler
    {
        public Regex CompileRegex(string fileExtension, FileSystemCasing casing)
        {
            var trimmed = fileExtension.TrimStart('.');
            if (fileExtension.Length - trimmed.Length > 1) throw new FileExtensionFormatException("Multiple leading dots are not permitted.", fileExtension);

            var regexPattern = $@"^.*\.{Regex.Escape(trimmed)}$";
            return FileSystemRegexHelpers.CreateRegex(regexPattern, casing);
        }
    }
}
