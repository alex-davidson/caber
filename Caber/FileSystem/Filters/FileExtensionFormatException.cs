using System;

namespace Caber.FileSystem.Filters
{
    public class FileExtensionFormatException : FormatException
    {
        public string FileExtension { get; }

        public FileExtensionFormatException(string message, string fileExtension) : base(message)
        {
            FileExtension = fileExtension;
        }
    }
}
