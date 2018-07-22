namespace Caber.Configuration.Storage.Validation
{
    public class InvalidFilterGlob : ConfigurationRuleViolation
    {
        public string Glob { get; }
        public string Message { get; }
        public int Position { get; }

        public InvalidFilterGlob(string glob, string message, int position)
        {
            Glob = glob;
            Message = message;
            Position = position;
        }
    }
}
