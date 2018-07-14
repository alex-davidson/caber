using System;
using Caber.Configuration;

namespace Caber.FileSystem.Validation
{
    /// <summary>
    /// The local root has already been used elsewhere.
    /// </summary>
    public class DuplicateLocalRootDeclaration : ConfigurationRuleViolation
    {
        public LocalRoot Node { get; }

        public DuplicateLocalRootDeclaration(LocalRoot node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }
    }
}
