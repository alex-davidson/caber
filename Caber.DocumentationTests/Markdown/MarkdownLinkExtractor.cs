using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Caber.Configuration;

namespace Caber.DocumentationTests.Markdown
{
    public class MarkdownLinkExtractor
    {
        public IList<Link> Extract(TextReader reader, string fileName = null) => ExtractInternal(reader, fileName).ToList();
        private readonly Regex rxMarkdownLink = new Regex(@"\[(.*?)\]\((.*?\.md)\)", RegexOptions.Compiled);

        private IEnumerable<Link> ExtractInternal(TextReader textReader, string fileName = null)
        {
            var reader = new LineReader(textReader);
            while (reader.Read())
            {
                var location = new ConfigurationLocation(fileName, reader.LineNumber);
                foreach (Match match in rxMarkdownLink.Matches(reader.Current))
                {
                    if (!match.Success) continue;   // Can this happen?
                    var caption = match.Groups[1].Value;
                    var path = match.Groups[2].Value;
                    yield return new Link(caption, path, location);
                }
            }
        }
    }
}
