namespace Caber.Configuration.Storage.Validation
{
    public class RelativePathRequired : ConfigurationRuleViolation
    {
        public string Path { get; }

        public RelativePathRequired(string path)
        {
            Path = path;
        }
    }
}
