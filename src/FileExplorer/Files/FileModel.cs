using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace FileExplorer.Files
{

    public class FileModel
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long? Size { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }

    }
}
