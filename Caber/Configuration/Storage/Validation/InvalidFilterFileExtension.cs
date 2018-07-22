namespace Caber.Configuration.Storage.Validation
{
    public class InvalidFilterFileExtension : ConfigurationRuleViolation
    {
        public string Extension { get; }
        public string Message { get; }

        public InvalidFilterFileExtension(string extension, string message)
        {
            Extension = extension;
            Message = message;
        }
    }
}
