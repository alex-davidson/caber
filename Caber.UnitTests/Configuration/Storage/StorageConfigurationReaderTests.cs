using System;
using System.Linq;
using Caber.Configuration.Storage;
using Caber.FileSystem;
using Caber.UnitTests.TestHelpers;
using NUnit.Framework;

namespace Caber.UnitTests.Configuration.Storage
{
    [TestFixture]
    public class StorageConfigurationReaderTests
    {
        [Test, CleanUp]
        public void CanReadSingleNamedRoot()
        {
            var builder = ReadConfiguration(@"
    <element>
      <add name=""root"" path=""c:\caber-root"" />
    </element>");
            var hierarchies = builder.BuildHierarchies();

            Assert.That(FindRoot(hierarchies, @"c:\caber-root\"), Is.Not.Null);
            Assert.That(hierarchies.NamedRoots,
                Is.EqualTo(new [] {
                    new NamedRoot("root", FindRoot(hierarchies, @"C:\caber-root\"))
                }));
        }

        [Test, CleanUp]
        public void CanReadNamedRootWithGraft()
        {
            var builder = ReadConfiguration(@"
    <element>
      <add name=""root"" path=""c:\caber-root"">
        <location path=""subdir\"" graft=""c:\grafted"" />
      </add>
    </element>");
            var hierarchies = builder.BuildHierarchies();

            Assert.That(FindRoot(hierarchies, @"C:\caber-root\"), Is.Not.Null);
            Assert.That(FindRoot(hierarchies, @"C:\grafted\"), Is.Not.Null);
            Assert.That(hierarchies.Grafts,
                Is.EqualTo(new [] {
                    new Graft(
                        new QualifiedPath(
                            FindRoot(hierarchies, @"C:\caber-root\"),
                            RelativePath.CreateFromSegments("subdir").AsContainer()),
                        FindRoot(hierarchies, @"C:\grafted\"))
                }).Using(default(Graft.DefaultEqualityComparer)));
        }

        [Test, CleanUp]
        public void CanReadNamedRootWithNestedGrafts()
        {
            var builder = ReadConfiguration(@"
    <element>
      <add name=""root"" path=""c:\caber-root"">
        <location path=""subdir\"" graft=""c:\grafted"">
           <location path=""leaf\"" graft=""c:\grafted-leaf"" />
        </location>
      </add>
    </element>");
            var hierarchies = builder.BuildHierarchies();

            Assert.That(FindRoot(hierarchies, @"C:\caber-root\"), Is.Not.Null);
            Assert.That(FindRoot(hierarchies, @"C:\grafted\"), Is.Not.Null);
            Assert.That(FindRoot(hierarchies, @"C:\grafted-leaf\"), Is.Not.Null);
            Assert.That(hierarchies.Grafts,
                Is.EqualTo(new [] {
                    new Graft(
                        new QualifiedPath(
                            FindRoot(hierarchies, @"C:\caber-root\"),
                            RelativePath.CreateFromSegments("subdir").AsContainer()),
                        FindRoot(hierarchies, @"C:\grafted\")),
                    new Graft(
                        new QualifiedPath(
                            FindRoot(hierarchies, @"C:\grafted\"),
                            RelativePath.CreateFromSegments("leaf").AsContainer()),
                        FindRoot(hierarchies, @"C:\grafted-leaf\"))
                }).Using(default(Graft.DefaultEqualityComparer)));
        }

        [Test, CleanUp]
        public void CanReadNamedRootWithGraftInsideLocation()
        {
            var builder = ReadConfiguration(@"
    <element>
      <add name=""root"" path=""c:\caber-root"">
        <location path=""subdir\"">
           <location path=""leaf\"" graft=""c:\grafted-leaf"" />
        </location>
      </add>
    </element>");
            var hierarchies = builder.BuildHierarchies();

            Assert.That(FindRoot(hierarchies, @"C:\caber-root\"), Is.Not.Null);
            Assert.That(FindRoot(hierarchies, @"C:\caber-root\subdir\"), Is.Not.Null);
            Assert.That(FindRoot(hierarchies, @"C:\grafted-leaf\"), Is.Not.Null);
            Assert.That(hierarchies.Grafts,
                Is.EqualTo(new [] {
                    new Graft(
                        new QualifiedPath(
                            FindRoot(hierarchies, @"C:\caber-root\"),
                            RelativePath.CreateFromSegments("subdir").AsContainer()),
                        FindRoot(hierarchies, @"C:\caber-root\subdir\")),
                    new Graft(
                        new QualifiedPath(
                            FindRoot(hierarchies, @"C:\caber-root\subdir\"),
                            RelativePath.CreateFromSegments("leaf").AsContainer()),
                        FindRoot(hierarchies, @"C:\grafted-leaf\"))
                }).Using(default(Graft.DefaultEqualityComparer)));
        }

        [Test, CleanUp]
        public void CanReadRootWithRegexFilter()
        {
            var builder = ReadConfiguration(@"
    <element>
      <add name=""root"" path=""c:\caber-root"">
        <filters>
          <match regex=""test.*"" rule=""exclude"" />
        </filters>
      </add>
    </element>");
            var hierarchies = builder.BuildHierarchies();

            var root = FindRoot(hierarchies, @"c:\caber-root\");
            var filter = hierarchies.GetFilterFor(root);
            Assert.That(filter.Exists, Is.True);
            Assert.That(filter.Evaluate(RelativePath.CreateFromSegments("test")).Description, Is.EqualTo("Regex(test.*)"));
        }

        private StorageHierarchiesBuilder ReadConfiguration(string xml)
        {
            var element = new TestConfigurationProvider<StorageElementCollection>().GetAsElement(xml);
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi(FileSystemCasing.CasePreservingInsensitive));
            var reader = new StorageConfigurationReader();
            reader.Read(element, builder);
            return builder;
        }

        private static LocalRoot FindRoot(StorageHierarchies hierarchies, string path) =>
            hierarchies.AllRoots.FirstOrDefault(r => r.RootUri == new Uri(path));
    }
}
