using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Caber.Configuration;

namespace Caber.DocumentationTests
{
    public class XmlBlock
    {
        public ConfigurationLocation Location { get; }
        public string OriginalText { get; }

        public XmlBlock(string originalText, ConfigurationLocation location)
        {
            OriginalText = originalText;
            Location = location;
        }

        public IList<XElement> GetElements() => XElement.Parse($"<xml>{OriginalText}</xml>").Elements().ToList();

        public override string ToString() => Location.ToString();
    }
}
