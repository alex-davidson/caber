using System.Collections.Generic;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem.Filters
{
    [TestFixture]
    public class FileExtensionToRegexCompilerTests
    {
        public static Case[] Cases = {
            new Case {
                Extension = ".txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^.*\.txt$",
                ShouldMatch = { "a.txt", "a/b/c.txt" },
                ShouldNotMatch = { "a", "a/b", "a.TXT", "a.log" },
            },
            new Case {
                Extension = ".txt",
                Casing = FileSystemCasing.CasePreservingInsensitive,
                ExpectedRegex = @"^.*\.txt$",
                ShouldMatch = { "a.txt", "a/b/c.txt", "a.TXT" },
                ShouldNotMatch = { "a", "a/b", "a.log" },
            },
            new Case {
                Extension = "txt",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^.*\.txt$",
                ShouldMatch = { "a.txt", "a/b/c.txt" },
                ShouldNotMatch = { "a", "a/b", "a.TXT", "a.log" },
            },

            // File extensions are always literals:
            new Case {
                Extension = "txt*",
                Casing = FileSystemCasing.CaseSensitive,
                ExpectedRegex = @"^.*\.txt\*$",
                ShouldMatch = { "a.txt*", "a/b/c.txt*" },
                ShouldNotMatch = { "a", "a/b", "a.TXT", "a.txtsomething", "a.log" },
            },
        };

        public class Case
        {
            public string Extension { get; set; }
            public FileSystemCasing Casing { get; set; }
            public string ExpectedRegex { get; set; }
            public IList<string> ShouldMatch { get; } = new List<string>();
            public IList<string> ShouldNotMatch { get; } = new List<string>();

            public override string ToString() => $"{Extension} ({Casing})";
        }

        [TestCaseSource(nameof(Cases))]
        public void FileExtensionAcceptsExpectedPaths(Case testCase)
        {
            var compiler = new FileExtensionToRegexCompiler();
            var regex = compiler.CompileRegex(testCase.Extension, testCase.Casing);

            Assert.Multiple(() => {
                foreach (var path in testCase.ShouldMatch)
                {
                    Assert.That(regex.IsMatch(path), Is.True);
                }
            });
        }

        [TestCaseSource(nameof(Cases))]
        public void FileExtensionRejectsExpectedPaths(Case testCase)
        {
            var compiler = new FileExtensionToRegexCompiler();
            var regex = compiler.CompileRegex(testCase.Extension, testCase.Casing);

            Assert.Multiple(() => {
                foreach (var path in testCase.ShouldNotMatch)
                {
                    Assert.That(regex.IsMatch(path), Is.False);
                }
            });
        }

        [TestCaseSource(nameof(Cases))]
        public void FileExtensionCompilesToExpectedRegex(Case testCase)
        {
            var compiler = new FileExtensionToRegexCompiler();
            var regex = compiler.CompileRegex(testCase.Extension, testCase.Casing);

            Assert.That(regex.ToString(), Is.EqualTo(testCase.ExpectedRegex));
        }
    }
}
