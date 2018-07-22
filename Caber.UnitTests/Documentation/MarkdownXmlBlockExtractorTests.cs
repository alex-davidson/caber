using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Caber.UnitTests.Documentation
{
    [TestFixture]
    public class MarkdownXmlBlockExtractorTests
    {
        [Test]
        public void ForMarkdownWithNoBlocks_ReturnsNoBlocks()
        {
            var reader = new StringReader(@"# Markdown
Contains no blocks.
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Is.Empty);
        }

        [Test]
        public void ForMarkdownWithUntypedBlock_ContainingXmlElements_ReturnsOneBlockContainingElements()
        {
            var reader = new StringReader(@"# Markdown
```
<a></a>
<b></b>
```
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks.Single().GetElements(), Has.Count.EqualTo(2));
        }

        [Test]
        public void ForMarkdownWithUntypedNonXmlBlock_ReturnsNoBlocks()
        {
            var reader = new StringReader(@"# Markdown
```
This is some text in a block.
```
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Is.Empty);
        }

        [Test]
        public void ForMarkdownWithUntypedInvalidXmlBlock_ReturnsNoBlocks()
        {
            var reader = new StringReader(@"# Markdown
```
This is some text which is not valid inner XML <_<,
in a block.
```
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Is.Empty);
        }

        [Test]
        public void ForMarkdownWithXmlTypedInvalidXmlBlock_ReturnsOneUnparseableBlock()
        {
            var reader = new StringReader(@"# Markdown
```xml
This is some text which is not valid inner XML <_<,
in a block which claims to be XML.
```
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.Catch<XmlException>(() => blocks.Single().GetElements());
        }

        [Test]
        public void ForMarkdownWithXmlTypedNonXmlBlock_ReturnsOneEmptyBlock()
        {
            var reader = new StringReader(@"# Markdown
```xml
This is some text in a block which claims to be XML.
```
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks.Single().GetElements(), Is.Empty);
        }

        [Test]
        public void ForMarkdownWithXmlTypedBlock_ContainingXmlElements_ReturnsOneBlockContainingElements()
        {
            var reader = new StringReader(@"# Markdown
```xml
<a></a>
<b></b>
```
");
            var blocks = new MarkdownXmlBlockExtractor().Extract(reader);
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks.Single().GetElements(), Has.Count.EqualTo(2));
        }
    }
}
