using System.IO;

namespace Caber.DocumentationTests.Markdown
{
    internal class LineReader
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
