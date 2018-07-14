using Caber.Configuration;

namespace Caber.FileSystem.Validation
{
    /// <summary>
    /// A local root contains another local root.
    /// </summary>
    public class OverlappingLocalRootDeclaration : ConfigurationRuleViolation
    {
        public LocalRoot Parent { get; }
        public LocalRoot Child { get; }

        public OverlappingLocalRootDeclaration(LocalRoot parent, LocalRoot child)
        {
            Parent = parent;
            Child = child;
        }
    }
}
