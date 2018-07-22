using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Caber.Configuration.Storage
{
    public class StorageElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new AddElement();
        protected override object GetElementKey(ConfigurationElement element) => ((AddElement)element).Id;

        protected override string ElementName => "add";
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        public IEnumerable<AddElement> Items => this.Cast<AddElement>();

        public class AddElement : LocationElementCollection
        {
            [ConfigurationProperty("name", IsRequired = true)]
            public string Name => (string)base["name"];

            [ConfigurationProperty("path", IsRequired = true)]
            public string Path => (string)base["path"];

            [ConfigurationProperty("filters")]
            public FilterCollection Filters => (FilterCollection)base["filters"];
        }
    }
}
