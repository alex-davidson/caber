using System.Collections.Generic;
using Caber.Configuration;

namespace Caber.Util
{
    public interface IErrorCollection : IEnumerable<ConfigurationRuleViolation>
    {
        int Count { get; }
        ErrorWatcher Watch();
    }
}
