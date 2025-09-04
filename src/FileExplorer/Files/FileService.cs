namespace FileExplorer.Files
{
    public interface IFileService
    {
        IEnumerable<FileModel> GetFiles( string relativePath);
        Stream GetFileStream(string relativePath);
        Task SaveFile( string relativePath, Stream fileStream);
        void DeleteFile( string relativePath);
        void CopyFile(string sourcePath, string destinationPath);
        void MoveFile( string sourcePath, string destinationPath);
        IEnumerable<FileModel> SearchFiles(string query);
    }
    public class FileService : IFileService
    {
        private readonly string _homeDir;

        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            // Use environment variable or fallback
            var envHome = Environment.GetEnvironmentVariable("FILE_EXPLORER_HOME") ?? "Default";
            _homeDir = Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", envHome);

            if (!Directory.Exists(_homeDir))
                Directory.CreateDirectory(_homeDir);
        }

        public IEnumerable<FileModel> GetFiles(string relativePath)
        {
            var homeFolder = _homeDir;
            var targetPath = Path.Combine(homeFolder, relativePath ?? string.Empty);
            var results = new List<FileModel>();

            if (!Directory.Exists(targetPath))
                return results;

            // Directories
            results.AddRange(Directory.GetDirectories(targetPath).Select(dir =>
            {
                var allFiles = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                var folderSize = allFiles.Sum(f => new FileInfo(f).Length);
                var folderCount = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Length;
                var fileCount = allFiles.Length;

                return new FileModel
                {
                    Name = Path.GetFileName(dir),
                    Path = Path.GetRelativePath(homeFolder, dir).Replace("\\", "/"),
                    IsDirectory = true,
                    Size = folderSize,
                    FileCount = fileCount,
                    FolderCount = folderCount
                };
            }));

            // Files
            results.AddRange(Directory.GetFiles(targetPath).Where(f => Path.GetFileName(f) != ".gitkeep").Select(file =>
            {
                var info = new FileInfo(file);
                return new FileModel
                {
                    Name = info.Name,
                    Path = Path.GetRelativePath(homeFolder, file).Replace("\\", "/"),
                    IsDirectory = false,
                    Size = info.Length
                };
            }));

            return results;
        }

        public Stream GetFileStream( string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));

            var homeFolder = _homeDir;
            var fullPath = Path.Combine(homeFolder, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("The specified file does not exist.", fullPath);

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public async Task SaveFile(string relativePath, Stream content)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
            var homeFolder = _homeDir;
            var targetPath = Path.Combine(homeFolder, relativePath ?? string.Empty);
            var dir = Path.GetDirectoryName(targetPath);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

            await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(fs);
        }
        public void DeleteFile( string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
            var homeFolder = _homeDir;
            var targetPath = Path.Combine(homeFolder, relativePath ?? string.Empty);
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            else if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            else
            {
                throw new FileNotFoundException("The specified file or directory does not exist.", targetPath);
            }

        }
        public void CopyFile(string sourcePath, string destinationPath)
        {
            var homeFolder = _homeDir;
            var fullSourcePath = Path.Combine(homeFolder, sourcePath);
            var fullDestinationPath = Path.Combine(homeFolder, destinationPath);

            if (File.Exists(fullSourcePath))
            {
                // Source is a file
                var destDir = Path.GetDirectoryName(fullDestinationPath);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir!);
                File.Copy(fullSourcePath, fullDestinationPath, overwrite: true);
            }
            else if (Directory.Exists(fullSourcePath))
            {
                // Source is a folder
                CopyDirectory(fullSourcePath, fullDestinationPath);
            }
            else
            {
                throw new FileNotFoundException("The source file or directory does not exist.", fullSourcePath);
            }
        }

        public void MoveFile( string sourcePath, string destinationPath)
        {
            var homeFolder = _homeDir;
            var fullSourcePath = Path.Combine(homeFolder, sourcePath);
            var fullDestinationPath = Path.Combine(homeFolder, destinationPath);

            if (File.Exists(fullSourcePath) || Directory.Exists(fullSourcePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullDestinationPath)!);
                Directory.Move(fullSourcePath, fullDestinationPath);
            }
            else
            {
                throw new FileNotFoundException("The source file or directory does not exist.", fullSourcePath);
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var sourceInfo = new DirectoryInfo(sourceDir);

            if (!sourceInfo.Exists) throw new DirectoryNotFoundException(sourceDir);

            // Preventing circular copy
            var fullSource = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullDest = Path.GetFullPath(destinationDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (fullDest.StartsWith(fullSource, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot copy a folder into itself or one of its subfolders.");
            }

            Directory.CreateDirectory(destinationDir);

            // Copy files
            foreach (var file in sourceInfo.GetFiles())
            {
                string destFile = Path.Combine(destinationDir, file.Name);
                file.CopyTo(destFile, true);
            }

            // Copy subdirectories recursively
            foreach (var subDir in sourceInfo.GetDirectories())
            {
                string destSubDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, destSubDir);
            }
        }


        public IEnumerable<FileModel> SearchFiles(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Enumerable.Empty<FileModel>();

            var homeFolder = _homeDir;

            if (!Directory.Exists(homeFolder))
                return Enumerable.Empty<FileModel>();

            var results = new List<FileModel>();

            // Recursively search directories
            foreach (var dir in Directory.GetDirectories(homeFolder, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(dir).Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new FileModel
                    {
                        Name = Path.GetFileName(dir),
                        Path = Path.GetRelativePath(homeFolder, dir).Replace("\\", "/"),
                        IsDirectory = true
                    });
                }
            }

            // Recursively search files
            foreach (var file in Directory.GetFiles(homeFolder, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file).Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    var info = new FileInfo(file);
                    results.Add(new FileModel
                    {
                        Name = info.Name,
                        Path = Path.GetRelativePath(homeFolder, file).Replace("\\", "/"),
                        IsDirectory = false,
                        Size = info.Length
                    });
                }
            }

            return results;
        }


    }
}
