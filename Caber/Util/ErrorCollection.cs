using System.Collections;
using System.Collections.Generic;
using Caber.Configuration;

namespace Caber.Util
{
    internal class ErrorCollection : IErrorCollection, IErrorReceiver
    {
        private readonly List<ConfigurationRuleViolation> errors = new List<ConfigurationRuleViolation>();

        public ConfigurationRuleViolation Record(ConfigurationRuleViolation violation)
        {
            errors.Add(violation);
            return violation;
        }

        public IEnumerator<ConfigurationRuleViolation> GetEnumerator() => errors.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => errors.Count;
        public ErrorWatcher Watch() => new ErrorWatcher(errors);
    }
}
