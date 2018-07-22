using System.Collections.Generic;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem.Filters
{
    [TestFixture]
    public class GlobToRegexCompilerTests
    {
        public static Case[] Cases = {
            // Literal text:
            new Case {
                Glob = "filename",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = "^filename$",
                ShouldMatch = { "filename" },
                ShouldNotMatch = { "filenames", "dir/filename", "Filename", "fIlEnAmE" },
            },
            new Case {
                Glob = "filename",
                Casing = FileSystemCasing.CasePreservingInsensitive,
                ExpectedRegex = "^filename$",
                ShouldMatch = { "filename", "Filename", "fIlEnAmE" },
                ShouldNotMatch = { "filenames", "dir/filename" },
            },

            // Path wildcard:
            new Case {
                Glob = "**/.caber/**",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^(.+/)?\.caber(/.+)?$",
                ShouldMatch = { ".caber", "a/.caber", ".caber/a", "a/b/c/.caber", ".caber/d/e/f","a/b/c/.caber/d/e/f" },
                ShouldNotMatch = { "a.caber", ".cabera", ".Caber", "a/b.caber", "a/.cabera", "a/.cabera/d/e" },
            },

            // Multi-character wildcard:
            new Case {
                Glob = "*/*.txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^[^/]*/[^/]*\.txt$",
                ShouldMatch = { "a/a.txt", "B/stuff.txt", "/stuff.txt", "a/.txt" },
                ShouldNotMatch = { "a/b/stuff.txt", "stuff.txt", "a/*.TXT" },
            },
            new Case {
                Glob = "*/subdir/*",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^[^/]*/subdir/[^/]*$",
                ShouldMatch = { "a/subdir/b.txt", "a/subdir/" },
                ShouldNotMatch = { "a/subdir", "a/b/subdir/d", "a/b/c" },
            },

            // Single character wildcard:
            new Case {
                Glob = "subdir?/file.txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^subdir[^/]/file\.txt$",
                ShouldMatch = { "subdir1/file.txt", "subdir-/file.txt" },
                ShouldNotMatch = { "a/subdir", "a/b/subdir/d", "a/b/c", "subdir-1/file.txt", "subdir1/something.txt" },
            },

            // Ranges:
            new Case {
                Glob = "[abc].txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^[abc]\.txt$",
                ShouldMatch = { "a.txt", "b.txt", "c.txt" },
                ShouldNotMatch = { "a/b.txt", "stuff.txt", "a.TXT", ".txt", "A.txt", "abc.txt" },
            },
            new Case {
                Glob = "[abc].txt",
                Casing = FileSystemCasing.CasePreservingInsensitive,
                ExpectedRegex = @"^[abc]\.txt$",
                ShouldMatch = { "a.txt", "b.txt", "c.txt", "a.TXT", "A.txt" },
                ShouldNotMatch = { "a/b.txt", "stuff.txt", ".txt", "abc.txt" },
            },
            new Case {
                Glob = "[!abc].txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^[^abc]\.txt$",
                ShouldMatch = { "d.txt", "e.txt", "f.txt", "A.txt" },
                ShouldNotMatch = { "a.txt", "b.txt", "c.txt", "a/b.txt", "stuff.txt", "a.TXT", ".txt", "abc.txt" },
            },
            new Case {
                Glob = "[!abc].txt",
                Casing = FileSystemCasing.CasePreservingInsensitive,
                ExpectedRegex = @"^[^abc]\.txt$",
                ShouldMatch = { "d.txt", "e.txt", "f.txt" },
                ShouldNotMatch = { "a.txt", "b.txt", "c.txt", "a/b.txt", "stuff.txt", "a.TXT", ".txt", "abc.txt", "A.txt" },
            },
            new Case {
                Glob = "[a-c].txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^[a-c]\.txt$",
                ShouldMatch = { "a.txt", "b.txt", "c.txt" },
                ShouldNotMatch = { "a/b.txt", "stuff.txt", "a.TXT", ".txt", "abc.txt" },
            },
            new Case {
                Glob = "[!a-c].txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^[^a-c]\.txt$",
                ShouldMatch = { "d.txt", "e.txt", "f.txt" },
                ShouldNotMatch = { "a.txt", "b.txt", "c.txt", "a/b.txt", "stuff.txt", "a.TXT", ".txt", "abc.txt" },
            },
        };

        public static ErrorCase[] ErrorCases = {
            // Ranges:
            new ErrorCase {
                Glob = "test[a",
                ErrorMessage = "Range at position 4 is not closed: [a",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[*",
                ErrorMessage = "Range at position 4 contains an invalid character: *",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[]",
                ErrorMessage = "Range at position 4 is empty: []",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[!]",
                ErrorMessage = "Range at position 4 is empty: [!]",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[a-]",
                ErrorMessage = "Range at position 4 is not valid: [a-]",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[-b]",
                ErrorMessage = "Range at position 4 is not valid: [-b]",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[!-b]",
                ErrorMessage = "Range at position 4 is not valid: [!-b]",
                ErrorPosition = 4,
            },
            new ErrorCase {
                Glob = "test[ab-c]",
                ErrorMessage = "Range at position 4 is not valid: [ab-c]",
                ErrorPosition = 4,
            },
        };

        [TestCaseSource(nameof(Cases))]
        public void GlobAcceptsExpectedPaths(Case testCase)
        {
            var compiler = new GlobToRegexCompiler();
            var regex = compiler.CompileRegex(testCase.Glob, testCase.Casing);

            Assert.Multiple(() => {
                foreach (var path in testCase.ShouldMatch)
                {
                    Assert.That(regex.IsMatch(path), Is.True);
                }
            });
        }

        [TestCaseSource(nameof(Cases))]
        public void GlobRejectsExpectedPaths(Case testCase)
        {
            var compiler = new GlobToRegexCompiler();
            var regex = compiler.CompileRegex(testCase.Glob, testCase.Casing);

            Assert.Multiple(() => {
                foreach (var path in testCase.ShouldNotMatch)
                {
                    Assert.That(regex.IsMatch(path), Is.False);
                }
            });
        }

        [TestCaseSource(nameof(Cases))]
        public void GlobCompilesToExpectedRegex(Case testCase)
        {
            var compiler = new GlobToRegexCompiler();
            var regex = compiler.CompileRegex(testCase.Glob, testCase.Casing);

            Assert.That(regex.ToString(), Is.EqualTo(testCase.ExpectedRegex));
        }

        [TestCaseSource(nameof(ErrorCases))]
        public void InvalidGlobGeneratesException(ErrorCase testCase)
        {
            var compiler = new GlobToRegexCompiler();

            var exception = Assert.Throws<GlobFormatException>(() => compiler.CompileRegex(testCase.Glob, FileSystemCasing.CaseSensitive));

            Assert.That(exception.Message, Is.EqualTo(testCase.ErrorMessage));
            Assert.That(exception.ErrorPosition, Is.EqualTo(testCase.ErrorPosition));
        }

        public class Case
        {
            public string Glob { get; set; }
            public FileSystemCasing Casing { get; set; }
            public string ExpectedRegex { get; set; }
            public IList<string> ShouldMatch { get; } = new List<string>();
            public IList<string> ShouldNotMatch { get; } = new List<string>();

            public override string ToString() => $"{Glob} ({Casing})";
        }

        public class ErrorCase
        {
            public string Glob { get; set; }
            public string ErrorMessage { get; set; }
            public int ErrorPosition { get; set; }

            public override string ToString() => $"{Glob} ({ErrorMessage})";
        }
    }
}
