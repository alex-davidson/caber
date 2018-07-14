using System;
using Caber.FileSystem;
using Caber.FileSystem.Validation;
using NUnit.Framework;

namespace Caber.UnitTests.FileSystem
{
    [TestFixture]
    public class StorageHierarchiesBuilderTests
    {
        [Test]
        public void GraftingOntoUndeclaredParent_ThrowsInvalidOperationException()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);

            Assert.Throws<InvalidOperationException>(() =>
                builder.AddGraftPoint(root, "Child", builder.CreateNode(@"C:\Other\A", FileSystemCasing.CasePreservingInsensitive)));
        }

        [Test]
        public void GraftingOntoUndeclaredParent_WithSamePathAsDeclaredParent_ThrowsInvalidOperationException()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);
            var fakeRoot = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);

            Assert.Throws<InvalidOperationException>(() =>
                builder.AddGraftPoint(fakeRoot, "Child", builder.CreateNode(@"C:\Other\A", FileSystemCasing.CasePreservingInsensitive)));
        }

        [Test]
        public void GraftingDuplicateQualifiedPath_RecordsDuplicateGraft()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);

            builder.AddGraftPoint(root, "Child", builder.CreateNode(@"C:\Other\A", FileSystemCasing.CasePreservingInsensitive));
            var error = builder.AddGraftPoint(root, "Child", builder.CreateNode(@"C:\Other\B", FileSystemCasing.CasePreservingInsensitive));

            Assert.That(error, Is.InstanceOf<DuplicateGraftDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void GraftingDuplicateChild_RecordsDuplicateLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);

            var child = builder.CreateNode(@"C:\Other", FileSystemCasing.CasePreservingInsensitive);
            builder.AddGraftPoint(root, @"Other\A", child);
            var error = builder.AddGraftPoint(root, @"Other\B", child);

            Assert.That(error, Is.InstanceOf<DuplicateLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void CreatingDuplicateNamedRoot_RecordsDuplicateNamedRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            builder.AddNamedRoot("Root", builder.CreateNode(@"C:\Root\A", FileSystemCasing.CasePreservingInsensitive));
            var error = builder.AddNamedRoot("Root", builder.CreateNode(@"C:\Root\B", FileSystemCasing.CasePreservingInsensitive));

            Assert.That(error, Is.InstanceOf<DuplicateNamedRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void CreatingDuplicateNamedRoot_DifferingByCase_RecordsDuplicateNamedRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            builder.AddNamedRoot("Root", builder.CreateNode(@"C:\Root\A", FileSystemCasing.CaseSensitive));
            var error = builder.AddNamedRoot("root", builder.CreateNode(@"C:\Root\B", FileSystemCasing.CaseSensitive));

            Assert.That(error, Is.InstanceOf<DuplicateNamedRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void CreatingDuplicateNamedRootLocation_DifferingByCase_RecordsDuplicateLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            builder.AddNamedRoot("RootA", builder.CreateNode(@"C:\Root\A", FileSystemCasing.CaseSensitive));
            var error = builder.AddNamedRoot("RootB", builder.CreateNode(@"C:\root\a", FileSystemCasing.CaseSensitive));

            Assert.That(error, Is.InstanceOf<DuplicateLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void MultipleNamesForTheSameRoot_RecordsDuplicateLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("RootA", root);
            var error = builder.AddNamedRoot("RootB", root);

            Assert.That(error, Is.InstanceOf<DuplicateLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void CreatingNamedRoot_ContainingExistingNamedRoot_RecordsOverlappingLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            builder.AddNamedRoot("RootA", builder.CreateNode(@"C:\Root\Child", FileSystemCasing.CasePreservingInsensitive));
            var error = builder.AddNamedRoot("RootB", builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive));

            Assert.That(error, Is.InstanceOf<OverlappingLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void GraftingLocalRoot_ContainingExistingNamedRoot_RecordsOverlappingLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);

            var error = builder.AddGraftPoint(root, "Child", builder.CreateNode(@"C:\", FileSystemCasing.CasePreservingInsensitive));

            Assert.That(error, Is.InstanceOf<OverlappingLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void GraftingLocalRoot_ContainedByExistingNamedRoot_RecordsOverlappingLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);

            // No-op graft is still an error.
            var error = builder.AddGraftPoint(root, "Child", builder.CreateNode(@"C:\Root\Child", FileSystemCasing.CasePreservingInsensitive));

            Assert.That(error, Is.InstanceOf<OverlappingLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void GraftingLocalRoot_ContainedByExistingGraftedLocalRoot_RecordsOverlappingLocalRoot()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);

            builder.AddGraftPoint(root, "A", builder.CreateNode(@"C:\Other", FileSystemCasing.CasePreservingInsensitive));
            var error = builder.AddGraftPoint(root, "B", builder.CreateNode(@"C:\Other\Child", FileSystemCasing.CasePreservingInsensitive));

            Assert.That(error, Is.InstanceOf<OverlappingLocalRootDeclaration>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }

        [Test]
        public void GraftingLocalRoot_OntoParentWithDifferingCasingRules_RecordsFileSystemCasingConflict()
        {
            var builder = new StorageHierarchiesBuilder(new StubFileSystemApi());
            var root = builder.CreateNode(@"C:\Root", FileSystemCasing.CasePreservingInsensitive);
            builder.AddNamedRoot("Root", root);

            var error = builder.AddGraftPoint(root, "Child", builder.CreateNode(@"C:\Other", FileSystemCasing.CaseSensitive));

            Assert.That(error, Is.InstanceOf<FileSystemCasingConflict>());
            Assert.That(builder.Errors, Is.EquivalentTo(new [] { error }));
        }
    }
}
