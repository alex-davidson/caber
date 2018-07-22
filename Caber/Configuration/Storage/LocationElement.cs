using System;
using System.Configuration;

namespace Caber.Configuration.Storage
{
    public class LocationElement : LocationElementCollection
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path => (string)base["path"];

        [ConfigurationProperty("graft", DefaultValue = null)]
        public string Graft => this.IsPropertyOmitted("graft") ? null : (string)base["graft"];

        [ConfigurationProperty("filters")]
        public FilterCollection Filters => (FilterCollection)base["filters"];
    }
}
