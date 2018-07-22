namespace Caber.Configuration.Storage.Validation
{
    public class AbsolutePathRequired : ConfigurationRuleViolation
    {
        public string Path { get; }

        public AbsolutePathRequired(string path)
        {
            Path = path;
        }
    }
}
