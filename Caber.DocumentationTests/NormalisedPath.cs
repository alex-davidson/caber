using System.IO;

namespace Caber.DocumentationTests
{
    public struct NormalisedPath
    {
        private readonly string path;

        public NormalisedPath(string path)
        {
            this.path = path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        }

        public static implicit operator string(NormalisedPath obj) => obj.ToString();

        public override string ToString() => path ?? "";
    }
}
