using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Caber.Configuration.Storage;
using Caber.FileSystem;
using Caber.UnitTests.TestHelpers;
using NUnit.Framework;

namespace Caber.UnitTests.Documentation
{
    [TestFixture]
    public class DocumentationTests
    {
        public static IEnumerable<XmlBlock> AllXmlBlocks => new EmbeddedMarkdownSource().GetAll().SelectMany(ReadXmlBlocks);

        [TestCaseSource(nameof(AllXmlBlocks))]
        [CleanUp]
        public void BlockIsValidConfiguration(XmlBlock block)
        {
            foreach (var xml in block.GetElements())
            {
                if (xml.Name == "storage")
                {
                    ValidateStorageConfiguration(xml);
                }
                else if (xml.Name == "filters")
                {
                    var storageXml = new XElement("storage",
                        new XElement("add",
                            new XAttribute("name", "root"),
                            new XAttribute("path", @"C:\Test")));
                    ValidateStorageConfiguration(storageXml);
                }
                else if (xml.Name == "senders")
                {
                    // Not implemented yet.
                }
                else if (xml.Name == "receivers")
                {
                    // Not implemented yet.
                }
                else if (xml.Name == "routes")
                {
                    // Not implemented yet.
                }
                else
                {
                    Assert.Inconclusive($"Not recognised as configuration: {xml.Name}");
                }
            }
        }

        private static void ValidateStorageConfiguration(XElement storageElement)
        {
            var element = new TestConfigurationProvider<StorageElementCollection>()
                .GetAsElement(CreateDummyElementFromInnerXml(storageElement));
            var builder = new StorageHierarchiesBuilder(new Configuration.StubFileSystemApi(FileSystemCasing.CasePreservingInsensitive));
            var reader = new StorageConfigurationReader();
            reader.Read(element, builder);
            builder.BuildHierarchies();

            Assert.That(reader.Errors, Is.Empty);
            Assert.That(builder.Errors, Is.Empty);
        }

        private static IList<XmlBlock> ReadXmlBlocks(EmbeddedMarkdown markdown)
        {
            using (var reader = markdown.Open())
            {
                return new MarkdownXmlBlockExtractor().Extract(reader, markdown.FileName);
            }
        }

        private static string CreateDummyElementFromInnerXml(XElement xml) => GetOuterXml(new XElement("element", xml.Nodes()));

        private static string GetOuterXml(XElement xml)
        {
            using (var reader = xml.CreateReader())
            {
                reader.MoveToContent();
                return reader.ReadOuterXml();
            }
        }
    }
}
