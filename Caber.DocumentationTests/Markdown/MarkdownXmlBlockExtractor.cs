using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Caber.Configuration;

namespace Caber.DocumentationTests.Markdown
{
    public class MarkdownXmlBlockExtractor
    {
        public IList<XmlBlock> Extract(TextReader reader, string fileName = null) => ExtractInternal(reader, fileName).ToList();

        private IEnumerable<XmlBlock> ExtractInternal(TextReader textReader, string fileName = null)
        {
            var reader = new LineReader(textReader);
            while (reader.Read())
            {
                if (!reader.Current.StartsWith("```")) continue;

                var location = new ConfigurationLocation(fileName, reader.LineNumber);
                var isXmlTyped = reader.Current.StartsWith("```xml", StringComparison.OrdinalIgnoreCase);

                var text = ReadBlock(reader);
                var block = new XmlBlock(text, location);
                try
                {
                    var elements = block.GetElements();
                    if (!isXmlTyped && !elements.Any()) continue;
                }
                catch (XmlException)
                {
                    if (!isXmlTyped) continue;
                }
                yield return block;
            }
        }

        private string ReadBlock(LineReader reader)
        {
            var lines = new List<string>();
            while (reader.Read())
            {
                if (reader.Current.StartsWith("```")) break;
                lines.Add(reader.Current);
            }
            return string.Join("\n", lines);
        }

        private class LineReader
        {
            private readonly TextReader reader;
            public int LineNumber { get; private set; }
            public string Current { get; private set; }

            public LineReader(TextReader reader)
            {
                this.reader = reader;
            }

            public bool Read()
            {
                Current = reader.ReadLine();
                if (Current == null)
                {
                    LineNumber = -1;
                    return false;
                }
                LineNumber++;
                return true;
            }
        }
    }
}
