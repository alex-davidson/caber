using System.Collections.Generic;
using Caber.Configuration;

namespace Caber.Util
{
    public interface IErrorReceiver : IEnumerable<ConfigurationRuleViolation>
    {
        ConfigurationRuleViolation Record(ConfigurationRuleViolation violation);
    }
}
