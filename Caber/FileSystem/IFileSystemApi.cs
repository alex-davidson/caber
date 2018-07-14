using System.Collections.Generic;

namespace Caber.FileSystem
{
    public interface IFileSystemApi
    {
        RelativePath GetCanonicalRelativePath(string rootPath, IEnumerable<string> relativePathParts);
        LocalRoot CreateStorageRoot(string fileSystemPath, FileSystemCasing casing);
    }
}
