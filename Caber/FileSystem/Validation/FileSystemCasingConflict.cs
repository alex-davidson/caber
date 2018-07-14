using Caber.Configuration;

namespace Caber.FileSystem.Validation
{
    /// <summary>
    /// The graft would add a local root which handles casing differently from its parent.
    /// </summary>
    public class FileSystemCasingConflict : ConfigurationRuleViolation
    {
        public Graft Graft { get; }

        public FileSystemCasingConflict(Graft graft)
        {
            Graft = graft;
        }
    }
}
