using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Caber.FileSystem.Filters
{
    public partial class GlobToRegexCompiler
    {
        private class Internal
        {
            private readonly StringBuilder builder = new StringBuilder();

            public string Compile(string globPattern)
            {
                builder.Clear();
                builder.Append("^");
                var reader = new GlobPatternReader(globPattern);
                do
                {
                    ReadLiteral(reader, BeginWildcardChars);
                    if (reader.IsEndOfPattern) break;
                    TryReadWildcard(reader);
                }
                while (!reader.IsEndOfPattern);
                builder.Append("$");
                return builder.ToString();
            }

            private static readonly char[] BeginWildcardChars = { '/', '*', '[', '?' };
            private static readonly char[] WildcardChars = { '/', '*', '[', '?', ']' };
            private static readonly Regex RxRange = new Regex(@"^!?(?!!)([^-]-[^-]|[^-]+)$", RegexOptions.Compiled);

            private void ReadLiteral(GlobPatternReader reader, char[] terminators)
            {
                var text = reader.ReadUntilAny(terminators);
                if (text.Length == 0) return;
                builder.Append(Regex.Escape(text));
            }

            private void TryReadWildcard(GlobPatternReader reader)
            {
                var start = reader.Position;
                if (reader.TryRead("/**/"))
                {
                    builder.Append("/(.+/)?");
                    return;
                }
                if (reader.TryRead("/**"))
                {
                    builder.Append("(/.+)?");
                    return;
                }
                if (reader.TryRead('/'))
                {
                    builder.Append("/");
                    return;
                }
                if (reader.TryRead("**/"))
                {
                    builder.Append("(.+/)?");
                    return;
                }
                if (reader.TryRead('*'))
                {
                    builder.Append("[^/]*");
                    return;
                }
                if (reader.TryRead('?'))
                {
                    builder.Append("[^/]");
                    return;
                }
                if (reader.TryRead('['))
                {
                    var range = reader.ReadUntilAny(WildcardChars);
                    if (!reader.TryRead(']'))
                    {
                        throw reader.IsEndOfPattern
                            ? new GlobFormatException($"Range at position {start} is not closed: {reader.GetSnippet(start, range.Length + 1)}", reader.Pattern, start)
                            : new GlobFormatException($"Range at position {start} contains an invalid character: {reader.GetSnippet(5)}", reader.Pattern, start);
                    }
                    if (range.Length == 0) throw new GlobFormatException($"Range at position {start} is empty: {reader.GetSnippet(start, 10)}", reader.Pattern, start);
                    var m = RxRange.Match(range);
                    if (range[0] == '!')
                    {
                        if (range.Length == 1) throw new GlobFormatException($"Range at position {start} is empty: {reader.GetSnippet(start, 10)}", reader.Pattern, start);
                    }
                    if (!m.Success) throw new GlobFormatException($"Range at position {start} is not valid: {reader.GetSnippet(start, range.Length + 2)}", reader.Pattern, start);

                    builder.Append('[');
                    if (range[0] == '!') builder.Append('^');
                    builder.Append(Regex.Escape(m.Groups[1].Value));
                    builder.Append(']');
                    return;
                }
                throw new GlobFormatException($"Unable to parse wildcard expression at position {start}: {reader.GetSnippet(start, 10)}", reader.Pattern, start);
            }

            private class GlobPatternReader
            {
                public string Pattern { get; }
                public int Position { get; private set; }
                public bool IsEndOfPattern => RemainingCharacters <= 0;
                private int RemainingCharacters => Pattern.Length - Position;

                public GlobPatternReader(string globPattern)
                {
                    this.Pattern = globPattern;
                }

                public string ReadUntilAny(char[] terminators)
                {
                    var text = ReadAheadUntilAny(terminators);
                    Position += text.Length;
                    return text;
                }

                public string ReadAheadUntilAny(char[] terminators)
                {
                    var terminatorIndex = Pattern.IndexOfAny(terminators, Position);
                    if (terminatorIndex < 0) return Pattern.Substring(Position);
                    var count = terminatorIndex - Position;
                    if (count <= 0) return "";
                    return Pattern.Substring(Position, count);
                }

                public bool TryRead(string wildcard)
                {
                    if (RemainingCharacters < wildcard.Length) return false;
                    if (Pattern.IndexOf(wildcard, Position, wildcard.Length, StringComparison.Ordinal) != Position) return false;
                    Position += wildcard.Length;
                    return true;
                }

                public bool TryRead(char wildcard)
                {
                    if (RemainingCharacters < 1) return false;
                    if (Pattern[Position] != wildcard) return false;
                    Position += 1;
                    return true;
                }

                public string GetSnippet(int count) => GetSnippet(Position, count);
                public string GetSnippet(int start, int count)
                {
                    var available = Pattern.Length - start;
                    if (count >= available) return Pattern.Substring(start, available);
                    return Pattern.Substring(start, count) + "...";
                }
            }
        }
    }
}
