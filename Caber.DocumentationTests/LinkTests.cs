using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caber.DocumentationTests.Markdown;
using NUnit.Framework;

namespace Caber.DocumentationTests
{
    [TestFixture]
    public class LinkTests
    {
        public static IEnumerable<Link> AllLinks => new EmbeddedMarkdownSource().GetAll().SelectMany(ReadLinks);

        [TestCaseSource(nameof(AllLinks))]
        public void LinkPathExists(Link link)
        {
            var relativeTo = Path.GetDirectoryName(link.Location.Source) ?? "";
            var resolved = new NormalisedPath(Path.Combine(relativeTo, link.Path));

            Assert.That(new EmbeddedMarkdownSource().GetAll().Select(m => m.FilePath), Has.One.EqualTo(resolved));
        }

        private static IList<Link> ReadLinks(EmbeddedMarkdown markdown)
        {
            using (var reader = markdown.Open())
            {
                return new MarkdownLinkExtractor().Extract(reader, markdown.FilePath);
            }
        }
    }
}
