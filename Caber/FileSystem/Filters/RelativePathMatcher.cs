using System.Text.RegularExpressions;

namespace Caber.FileSystem.Filters
{
    public struct RelativePathMatcher
    {
        private readonly Regex regex;
        private readonly string description;

        public string Description => description ?? "default";
        public FilterRule Rule { get; }

        public RelativePathMatcher(Regex regex, FilterRule rule, string description = null) : this()
        {
            this.regex = regex;
            this.description = description ?? $"Regex({regex})";
            this.Rule = rule;
        }

        public RelativePathMatcher(FilterRule rule) : this()
        {
            this.Rule = rule;
        }

        public bool Matches(RelativePath relativePath)
        {
            if (regex == null) return true; // default(RelativePathMatcher) should match all paths.
            return regex.IsMatch(relativePath.ToString());
        }

        public override string ToString() => $"{Description} -> {Rule}";
    }
}
