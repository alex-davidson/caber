using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace Caber.Configuration.Storage
{
    public abstract class LocationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new LocationElement();
        protected override object GetElementKey(ConfigurationElement element) => ((LocationElement)element).Id;

        protected override string ElementName => "location";
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        public IEnumerable<LocationElement> Children => this.Cast<LocationElement>();

        public Guid Id { get; } = Guid.NewGuid();

        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            // Ignore unrecognised elements for now.
            base.OnDeserializeUnrecognizedElement(elementName, reader);
            return true;
        }
    }
}
