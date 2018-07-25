using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Caber.DocumentationTests.Markdown
{
    public class EmbeddedMarkdownSource
    {
        private readonly string resourceNamespace;
        private readonly Assembly assembly;

        public EmbeddedMarkdownSource(Type marker = null)
        {
            marker = marker ?? GetType();
            resourceNamespace = marker.Namespace ?? throw new ArgumentException($"Cannot determine namespace to search from type {marker}");
            assembly = marker.Assembly;
        }

        public IEnumerable<EmbeddedMarkdown> GetAll()
        {
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(resourceNamespace + ".")) continue;
                var fileName = resourceName.Substring(resourceNamespace.Length + 1);
                if (Path.GetExtension(fileName) != ".md") continue;
                yield return new EmbeddedMarkdown(assembly, resourceName, fileName);
            }
        }
    }
}
