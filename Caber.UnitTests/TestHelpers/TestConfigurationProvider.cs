using System;
using System.Configuration;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace Caber.UnitTests.TestHelpers
{
    public class TestConfigurationProvider<T>
    {
        public const int FirstLineNumber = 7;
        public const string ElementName = "element";

        private static readonly Type SectionType = typeof(Section);

        private string GenerateConfiguration(string elementXml)
        {
            var element = XElement.Parse(elementXml);
            if (element.Name != ElementName) throw new ArgumentException("Root XML element must be called 'element'.");

            return $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <configSections>
    <section name=""section"" type=""{SectionType.FullName}, {SectionType.Assembly.GetName().Name}"" />
  </configSections>
  <section>
    {elementXml}
  </section>
</configuration>";
        }

        public T GetAsElement(string xml)
        {
            var file = TemporaryFile.CreateNew();
            TestContext.CurrentContext.DisposeAfterTest(file);
            File.WriteAllText(file.FullPath, GenerateConfiguration(xml));
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(
                new ExeConfigurationFileMap { ExeConfigFilename = file.FullPath },
                ConfigurationUserLevel.None);
            var section = (Section)configuration.GetSection("section");
            return section.Element;
        }

        public class Section : ConfigurationSection
        {
            [ConfigurationProperty(ElementName)]
            public T Element => (T)base[ElementName];
        }
    }
}
