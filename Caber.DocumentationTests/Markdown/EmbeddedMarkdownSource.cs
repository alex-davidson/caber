using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Caber.DocumentationTests.Markdown
{
    public class EmbeddedMarkdownSource
    {
        public IEnumerable<EmbeddedMarkdown> GetAll(Type marker = null)
        {
            marker = marker ?? GetType();
            var resourceNamespace = marker.Namespace ?? throw new ArgumentException($"Cannot determine namespace to search from type {marker}");
            return GetFrom(marker.Assembly, resourceNamespace);
        }

        private static IEnumerable<EmbeddedMarkdown> GetFrom(Assembly assembly, string resourceNamespace)
        {
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(resourceNamespace + ".")) continue;
                var fileName = resourceName.Substring(resourceNamespace.Length + 1);
                if (Path.GetExtension(fileName) != ".md") continue;
                yield return new EmbeddedMarkdown(assembly, resourceNamespace, fileName);
            }
        }
    }
}
