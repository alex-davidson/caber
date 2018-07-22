using System;

namespace Caber.FileSystem
{
    /// <summary>
    /// Filesystem location declared as a root of a named abstract hierarchy.
    /// </summary>
    public struct NamedRoot
    {
        public string Name { get; }
        public LocalRoot LocalRoot { get; }

        public NamedRoot(string name, LocalRoot localRoot)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            Name = name;
            LocalRoot = localRoot ?? throw new ArgumentNullException(nameof(localRoot));
        }

        public override string ToString() => $"[{Name}] {LocalRoot}";
    }
}
