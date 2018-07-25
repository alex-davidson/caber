using System.IO;
using System.Reflection;

namespace Caber.DocumentationTests.Markdown
{
    public class EmbeddedMarkdown
    {
        public string FileName { get; }
        private readonly Assembly assembly;
        private readonly string resourceNamespace;

        public EmbeddedMarkdown(Assembly assembly, string resourceNamespace, string fileName)
        {
            FileName = fileName;
            this.assembly = assembly;
            this.resourceNamespace = resourceNamespace;
        }

        private Stream GetValidStream()
        {
            var stream = assembly.GetManifestResourceStream(string.Concat(resourceNamespace, ".", FileName));
            if (stream != null) return stream;
            throw new FileNotFoundException($"Resource does not exist for the specified file: {FileName}");
        }

        public TextReader Open() => new StreamReader(GetValidStream());
    }
}
