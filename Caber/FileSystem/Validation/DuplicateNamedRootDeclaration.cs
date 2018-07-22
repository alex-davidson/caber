using Caber.Configuration;

namespace Caber.FileSystem.Validation
{
    /// <summary>
    /// This root name has been declared already.
    /// </summary>
    public class DuplicateNamedRootDeclaration : ConfigurationRuleViolation
    {
        public NamedRoot Duplicate { get; }
        public NamedRoot Original { get; }

        public DuplicateNamedRootDeclaration(NamedRoot duplicate, NamedRoot original)
        {
            Duplicate = duplicate;
            Original = original;
        }
    }
}
