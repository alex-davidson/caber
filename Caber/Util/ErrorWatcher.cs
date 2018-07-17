using System.Collections.Generic;
using System.Linq;
using Caber.Configuration;

namespace Caber.Util
{
    public struct ErrorWatcher
    {
        private IEnumerable<ConfigurationRuleViolation> errors;
        private int initialErrorCount;

        internal ErrorWatcher(IEnumerable<ConfigurationRuleViolation> errors)
        {
            this.errors = errors;
            initialErrorCount = errors.Count();
        }

        public bool HasNewErrors => errors?.Count() > initialErrorCount;

        public static ErrorWatcher operator +(ErrorWatcher a, ErrorWatcher b)
        {
            return new ErrorWatcher {
                errors = a.errors?.Concat(b.errors) ?? b.errors,
                initialErrorCount = a.initialErrorCount + b.initialErrorCount
            };
        }
    }
}
