namespace Caber.Configuration
{
    public struct ConfigurationLocation
    {
        public ConfigurationLocation(string source, int lineNumber)
        {
            Source = source;
            LineNumber = lineNumber;
        }

        public string Source { get; }
        public int LineNumber { get; }

        public override string ToString() => $"{Source ?? "<unknown>"}:line {LineNumber}";
    }
}
