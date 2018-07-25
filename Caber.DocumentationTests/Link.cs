using Caber.Configuration;

namespace Caber.DocumentationTests
{
    public class Link
    {
        public ConfigurationLocation Location { get; }
        public string Caption { get; }
        public NormalisedPath Path { get; }

        public Link(string caption, string path, ConfigurationLocation location)
        {
            Caption = caption;
            Path = new NormalisedPath(path);
            Location = location;
        }

        public override string ToString() => $"{Location} => {Path}";
    }
}
