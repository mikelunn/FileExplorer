namespace FileExplorer.Files
{
    public interface IFileService
    {
        IEnumerable<FileModel> GetFiles(string home, string relativePath);
        Stream GetFileStream(string home, string relativePath);
        Task SaveFile(string home, string relativePath, Stream fileStream);
        void DeleteFile(string home, string relativePath);
        void CopyFile(string home, string sourcePath, string destinationPath);
        void MoveFile(string home, string sourcePath, string destinationPath);
        IEnumerable<FileModel> SearchFiles(string home, string query);
    }
    public class FileService(IWebHostEnvironment webHostEnvironment) : IFileService
    {
        private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;

        private string GetHomeFolderPath(string home)
        {
            // Sanitize the home input to prevent directory traversal attacks
            var sanitizedHome = string.Join("_", home.Split(Path.GetInvalidFileNameChars()));
            var homePath = Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", sanitizedHome);
            // Ensure the directory exists
            if (!Directory.Exists(homePath))
            {
                throw new DirectoryNotFoundException($"The home directory '{sanitizedHome}' does not exist.");
            }
            return homePath;
        }
        public IEnumerable<FileModel> GetFiles(string home, string relativePath)
        {
            var homeFolder = GetHomeFolderPath(home);
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
                    Home = home,
                    Size = folderSize,
                    FileCount = fileCount,
                    FolderCount = folderCount
                };
            }));

            // Files
            results.AddRange(Directory.GetFiles(targetPath).Select(file =>
            {
                var info = new FileInfo(file);
                return new FileModel
                {
                    Name = info.Name,
                    Path = Path.GetRelativePath(homeFolder, file).Replace("\\", "/"),
                    IsDirectory = false,
                    Home = home,
                    Size = info.Length
                };
            }));

            return results;
        }

        public Stream GetFileStream(string home, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));

            var homeFolder = GetHomeFolderPath(home);
            var fullPath = Path.Combine(homeFolder, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("The specified file does not exist.", fullPath);

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public async Task SaveFile(string home, string relativePath, Stream content)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
            var homeFolder = GetHomeFolderPath(home);
            var targetPath = Path.Combine(homeFolder, relativePath ?? string.Empty);
            var dir = Path.GetDirectoryName(targetPath);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

            await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(fs);
        }
        public void DeleteFile(string home, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
            var homeFolder = GetHomeFolderPath(home);
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
        public void CopyFile(string home, string sourcePath, string destinationPath)
        {
            var homeFolder = GetHomeFolderPath(home);
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

        public void MoveFile(string home, string sourcePath, string destinationPath)
        {
            var homeFolder = GetHomeFolderPath(home);
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

            // Prevent circular copy
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


        public IEnumerable<FileModel> SearchFiles(string home, string query)
        {
            if (string.IsNullOrEmpty(query))
                return Enumerable.Empty<FileModel>();

            var homeFolder = GetHomeFolderPath(home);

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
                        IsDirectory = true,
                        Home = home
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
                        Home = home,
                        Size = info.Length
                    });
                }
            }

            return results;
        }


    }
}
