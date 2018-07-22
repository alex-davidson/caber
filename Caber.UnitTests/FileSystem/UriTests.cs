using System;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    /// <summary>
    /// Define platform behaviours which we rely upon.
    /// </summary>
    [TestFixture]
    public class UriTests
    {
        [Test]
        public void ComparesUNCHostNamesCaseInsensitively()
        {
            var a = new Uri(@"\\computer\share");
            var b = new Uri(@"\\COMPUTER\share");

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ComparesUNCSharesCaseInsensitively()
        {
            var a = new Uri(@"\\computer\share");
            var b = new Uri(@"\\computer\SHARE");

            Assert.That(a, Is.EqualTo(b));
        }

        /// <summary>
        /// I'm not sure this is strictly correct, since the remote machine could have a
        /// case-sensitive filesystem or even be running a completely different OS. But this is the
        /// behaviour we currently need to tolerate.
        /// </summary>
        [Test]
        public void ComparesUNCPathSegmentsCaseInsensitively()
        {
            var a = new Uri(@"\\computer\share\a\b\c");
            var b = new Uri(@"\\computer\share\A\B\C");

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ComparesDriveLettersCaseInsensitively()
        {
            var a = new Uri(@"c:\");
            var b = new Uri(@"C:\");

            Assert.That(a, Is.EqualTo(b));
        }

        /// <summary>
        /// I'm not sure this is strictly correct, since the E: drive or any subdirectory could
        /// have a case-sensitive filesystem. But this is the behaviour we currently need to tolerate.
        /// </summary>
        [Test]
        public void ComparesLocalPathSegmentsCaseInsensitively()
        {
            var a = new Uri(@"E:\a\b\c");
            var b = new Uri(@"E:\A\B\C");

            Assert.That(a, Is.EqualTo(b));
        }

        /// <summary>
        /// Sanity check. If the Uri class got this wrong it would not actually be much use for anything.
        /// </summary>
        [Test]
        public void ComparesHTTPPathSegmentsCaseSensitively()
        {
            var a = new Uri("http://example.com/a/b/c");
            var b = new Uri("http://example.com/A/B/C");

            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
