using Caber.FileSystem.Filters;

namespace Caber.Configuration.Storage.Validation
{
    public class InvalidFilterRegex : ConfigurationRuleViolation
    {
        public string Regex { get; }
        public string Message { get; }

        public InvalidFilterRegex(string regex, string message)
        {
            Regex = regex;
            Message = message;
        }
    }
}
