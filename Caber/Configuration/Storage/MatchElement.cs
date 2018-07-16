using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using Caber.FileSystem.Filters;

namespace Caber.Configuration.Storage
{
    public class MatchElement : ConfigurationElement
    {
        public string Pattern
        {
            get
            {
                if (Regex != null) return $"Regex:{Regex}";
                if (Glob != null) return $"Glob:{Glob}";
                if (Extension != null) return $"Extension:{Extension}";
                return "default";
            }
        }

        [ConfigurationProperty("regex", DefaultValue = null)]
        public string Regex => this.IsPropertyOmitted("regex") ? null : (string)base["regex"];
        [ConfigurationProperty("glob", DefaultValue = null)]
        public string Glob => this.IsPropertyOmitted("glob") ? null : (string)base["glob"];
        [ConfigurationProperty("extension", DefaultValue = null)]
        public string Extension => this.IsPropertyOmitted("extension") ? null : (string)base["extension"];

        [ConfigurationProperty("rule", IsRequired = true)]
        [TypeConverter(typeof(CaseInsensitiveEnumConfigurationConverter<FilterRule>))]
        public FilterRule Rule => (FilterRule)base["rule"];
    }

    public class FilterCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new MatchElement();
        protected override object GetElementKey(ConfigurationElement element) => ((MatchElement)element).Pattern;

        protected override string ElementName => "match";
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        public IEnumerable<MatchElement> Filters => this.Cast<MatchElement>();
    }
}
