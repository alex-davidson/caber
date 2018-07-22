using System.Configuration;

namespace Caber.Configuration
{
    public abstract class ConfigurationRuleViolation
    {
        public ConfigurationLocation Location { get; private set; }
        public void SetLocation(ConfigurationLocation location) => Location = location;
        public void SetLocation(ElementInformation location) => Location = new ConfigurationLocation(location.Source, location.LineNumber);
    }
}
