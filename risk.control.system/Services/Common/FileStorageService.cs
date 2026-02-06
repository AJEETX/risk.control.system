namespace risk.control.system.Services.Common
{
    public interface IFileStorageService
    {
        Task<(string FileName, string RelativePath)> SaveAsync(IFormFile file, string category, string? subFolder = null, string? subSubFolder = null, string[]? allowedTypes = null);
        Task<(string FileName, string RelativePath)> SaveAsync(byte[] data, string extension, string category, string? subFolder = null, string? subSubFolder = null, string[]? allowedExtensions = null);
        Task<(string FileName, string RelativePath)> SaveMediaAsync(IFormFile file, string category, string? subFolder = null, string? subSubFolder = null);
    }
    internal class FileStorageService : IFileStorageService
    {
        private const string RootFolder = "Document";
        private readonly IWebHostEnvironment env;
        public FileStorageService(IWebHostEnvironment env)
        {
            this.env = env;
        }
        public async Task<(string FileName, string RelativePath)> SaveMediaAsync(IFormFile file, string category, string? subFolder = null, string? subSubFolder = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            // Allowed audio and video MIME types
            string[] allowedTypes =
            {
                // Audio
                "audio/mpeg", "audio/wav", "audio/x-wav",
                "audio/ogg", "audio/aac", "audio/flac",

                // Video
                "video/mp4", "video/mpeg", "video/ogg",
                "video/webm", "video/quicktime",
                "video/x-msvideo", "video/x-ms-wmv"
             };

            // Allowed extensions
            string[] allowedExt =
            {
                ".mp3", ".wav", ".ogg", ".aac", ".flac",
                ".mp4", ".mpeg", ".webm", ".mov", ".avi", ".wmv"
            };

            if (!allowedTypes.Contains(file.ContentType))
                throw new InvalidOperationException($"Unsupported type: {file.ContentType}");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExt.Contains(ext))
                throw new InvalidOperationException($"Unsupported file extension: {ext}");

            var fileName = $"{Guid.NewGuid()}{ext}";
            var folderPath = GetOrCreateFolder(category, subFolder, subSubFolder);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var relativePath = BuildRelativePath(category, subFolder, subSubFolder, fileName);

            return (fileName, relativePath);
        }

        public async Task<(string FileName, string RelativePath)> SaveAsync(IFormFile file, string category, string? subFolder = null, string? subSubFolder = null, string[]? allowedTypes = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            allowedTypes ??= new[] { "application/zip", "application/x-zip-compressed", "multipart/x-zip", "image/jpeg", "image/png" };

            if (!allowedTypes.Contains(file.ContentType))
                throw new InvalidOperationException($"Unsupported content type: {file.ContentType}");
            var allowedExt = new[] { ".zip", ".jpg", ".jpeg", ".png" };

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                throw new InvalidOperationException($"Unsupported file extension: {ext}");

            var fileName = $"{Guid.NewGuid()}{ext}";

            var folderPath = GetOrCreateFolder(category, subFolder, subSubFolder);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relative = BuildRelativePath(category, subFolder, subSubFolder, fileName);

            return (fileName, relative);
        }

        public async Task<(string FileName, string RelativePath)> SaveAsync(byte[] data, string extension, string category, string? subFolder = null, string? subSubFolder = null, string[]? allowedExtensions = null)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Invalid data bytes");

            extension = extension.StartsWith(".") ? extension : "." + extension;

            allowedExtensions ??= new[] { ".jpg", ".jpeg", ".png" };

            if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
                throw new InvalidOperationException($"Unsupported extension: {extension}");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = GetOrCreateFolder(category, subFolder, subSubFolder);
            var filePath = Path.Combine(folderPath, fileName);

            await File.WriteAllBytesAsync(filePath, data);

            var relative = BuildRelativePath(category, subFolder, subSubFolder, fileName);

            return (fileName, relative);
        }

        private string GetOrCreateFolder(string category, string? subFolder, string? subSubFolder)
        {
            var folder = Path.Combine(env.ContentRootPath, RootFolder,        // ALWAYS start with Document/
                category           // Policy, Agency, Company
            );

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (!string.IsNullOrWhiteSpace(subFolder))
            {
                folder = Path.Combine(folder, subFolder);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }
            if (!string.IsNullOrWhiteSpace(subSubFolder))
            {
                folder = Path.Combine(folder, subSubFolder);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }
            return folder;
        }

        private static string BuildRelativePath(string category, string? subFolder, string? subSubFolder, string fileName)
        {
            var path = Path.Combine(RootFolder, category, subFolder ?? "", subSubFolder ?? "", fileName).Replace("\\", "/");

            return path;
        }
    }
}
