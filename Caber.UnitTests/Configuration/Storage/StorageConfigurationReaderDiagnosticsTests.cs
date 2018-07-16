using System;
using System.Collections.Generic;
using System.Linq;
using Caber.Configuration;
using Caber.Configuration.Storage;
using Caber.Configuration.Storage.Validation;
using Caber.FileSystem;
using Caber.FileSystem.Validation;
using Caber.UnitTests.TestHelpers;
using NUnit.Framework;

namespace Caber.UnitTests.Configuration.Storage
{
    [TestFixture]
    public class StorageConfigurationReaderDiagnosticsTests
    {
        private static int GetActualLineNumber(int zeroBasedFromTestCase) => TestConfigurationProvider<object>.FirstLineNumber + zeroBasedFromTestCase;

        [Test, CleanUp]
        public void DuplicateNamedRoot_Propagates_BuilderError()
        {
            var builder = ReadConfiguration(out _, @"
    <element>
      <add name=""root"" path=""c:\caber-root"" />
      <add name=""root"" path=""c:\caber-other"" />
    </element>");
            builder.BuildHierarchies();

            ExpectErrors(builder.Errors,
                Match.Object<DuplicateNamedRootDeclaration>(r =>
                    r.Location.LineNumber == GetActualLineNumber(3) &&
                    r.Original.LocalRoot.RootUri == new Uri(@"c:\caber-root\") &&
                    r.Duplicate.LocalRoot.RootUri == new Uri(@"c:\caber-other\")));
        }

        [Test, CleanUp]
        public void InvalidNamedRootPath_Yields_AbsolutePathRequiredError()
        {
            var builder = ReadConfiguration(out var reader, @"
    <element>
      <add name=""root"" path=""..\caber-root"" />
    </element>");
            builder.BuildHierarchies();

            ExpectErrors(reader.Errors,
                Match.Object<AbsolutePathRequired>(r =>
                    r.Location.LineNumber == GetActualLineNumber(2) &&
                    r.Path == @"..\caber-root"));
        }

        [Test, CleanUp]
        public void CanReportMultipleReaderErrorsFromSiblingHierarchies()
        {
            var builder = ReadConfiguration(out var reader, @"
    <element>
      <add name=""root1"" path=""..\caber-root"" />
      <add name=""root2"" path=""..\caber-other"" />
    </element>");
            builder.BuildHierarchies();

            ExpectErrors(reader.Errors,
                Match.Object<AbsolutePathRequired>(r =>
                    r.Location.LineNumber == GetActualLineNumber(2) &&
                    r.Path == @"..\caber-root"),
                Match.Object<AbsolutePathRequired>(r =>
                    r.Location.LineNumber == GetActualLineNumber(3) &&
                    r.Path == @"..\caber-other"));
        }

        [Test, CleanUp]
        public void CanReportReaderErrorsFromFilterOfInvalidRoot()
        {
            var builder = ReadConfiguration(out var reader, @"
    <element>
      <add name=""root"" path=""..\caber-root"">
        <filters>
          <match extension=""...."" rule=""exclude"" />
        </filters>
      </add>
    </element>");
            builder.BuildHierarchies();

            Assert.That(reader.Errors, Does.Contain(
                Match.Object<InvalidFilterFileExtension>(r =>
                    r.Location.LineNumber == GetActualLineNumber(4) &&
                    r.Extension == "....")));
        }

        [Test, CleanUp]
        public void CanReportReaderErrorsFromChildOfInvalidRoot()
        {
            var builder = ReadConfiguration(out var reader, @"
    <element>
      <add name=""root1"" path=""..\caber-root"">
        <location path=""c:\subdir\"" graft=""c:\grafted"" />
      </add>
    </element>");
            builder.BuildHierarchies();

            Assert.That(reader.Errors, Does.Contain(
                Match.Object<RelativePathRequired>(r =>
                    r.Location.LineNumber == GetActualLineNumber(3) &&
                    r.Path == @"c:\subdir\")));
        }

        [Test, CleanUp]
        public void CanReportReaderErrorsFromFiltersOfChildOfInvalidRoot()
        {
            var builder = ReadConfiguration(out var reader, @"
    <element>
      <add name=""root1"" path=""..\caber-root"">
        <location path=""c:\subdir\"" graft=""c:\grafted"">
          <filters>
            <match extension=""...."" rule=""exclude"" />
          </filters>
        </location>
      </add>
    </element>");
            builder.BuildHierarchies();

            Assert.That(reader.Errors, Does.Contain(
                Match.Object<InvalidFilterFileExtension>(r =>
                    r.Location.LineNumber == GetActualLineNumber(5) &&
                    r.Extension == "....")));
        }

        private StorageHierarchiesBuilder ReadConfiguration(out StorageConfigurationReader reader, string xml)
        {
            var element = new TestConfigurationProvider<StorageElementCollection>().GetAsElement(xml);
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi(FileSystemCasing.CasePreservingInsensitive));
            reader = new StorageConfigurationReader();
            reader.Read(element, builder);
            return builder;
        }

        private static void ExpectErrors(IEnumerable<ConfigurationRuleViolation> actualErrors, params object[] expectedErrors)
        {
            Assert.That(actualErrors, Is.EquivalentTo(expectedErrors));
        }
    }
}
