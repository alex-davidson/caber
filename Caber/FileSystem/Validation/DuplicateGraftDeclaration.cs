using System;
using Caber.Configuration;

namespace Caber.FileSystem.Validation
{
    /// <summary>
    /// The graft point has already been declared elsewhere.
    /// </summary>
    public class DuplicateGraftDeclaration : ConfigurationRuleViolation
    {
        public Graft Duplicate { get; }
        public Graft Original { get; }

        public DuplicateGraftDeclaration(Graft duplicate, Graft original)
        {
            Duplicate = duplicate ?? throw new ArgumentNullException(nameof(duplicate));
            Original = original ?? throw new ArgumentNullException(nameof(original));
        }
    }
}
