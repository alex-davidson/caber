using System.IO;
using System.Reflection;

namespace Caber.DocumentationTests.Markdown
{
    public class EmbeddedMarkdown
    {
        public NormalisedPath FilePath { get; }
        private readonly Assembly assembly;
        private readonly string resourceName;

        public EmbeddedMarkdown(Assembly assembly, string resourceName, string fileName)
        {
            FilePath = new NormalisedPath(fileName);
            this.assembly = assembly;
            this.resourceName = resourceName;
        }

        private Stream GetValidStream()
        {
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null) return stream;
            throw new FileNotFoundException($"Resource does not exist for the specified file: {FilePath}");
        }

        public TextReader Open() => new StreamReader(GetValidStream());

        public override string ToString() => FilePath;
    }
}
