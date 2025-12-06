namespace risk.control.system.Services
{
    public interface IFileStorageService
    {
        Task<(string FileName, string RelativePath)> SavePolicyDocumentAsync(IFormFile file, string? subFolder = null);
        Task<(string FileName, string RelativePath)> SavePolicyDocumentAsync(byte[] data, string extension, string? subFolder = null);
    }
    internal class FileStorageService : IFileStorageService
    {
        private readonly string _rootPath;
        private readonly IWebHostEnvironment env;
        public FileStorageService(IWebHostEnvironment env)
        {
            this.env = env;
        }

        public async Task<(string FileName, string RelativePath)> SavePolicyDocumentAsync(IFormFile file, string? subFolder = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            // Validate content type
            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType))
                throw new InvalidOperationException($"Unsupported format: {file.ContentType}");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var folder = GetFolder(subFolder);
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("CaseData", "PolicyDocuments", subFolder ?? "", fileName)
                .Replace("\\", "/");

            return (fileName, relativePath);
        }
        public async Task<(string FileName, string RelativePath)> SavePolicyDocumentAsync(byte[] data, string extension, string? subFolder = null)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Invalid document bytes");

            extension = extension.StartsWith(".") ? extension : "." + extension;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
                throw new InvalidOperationException($"Unsupported extension: {extension}");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folder = GetFolder(subFolder);
            var filePath = Path.Combine(folder, fileName);

            await File.WriteAllBytesAsync(filePath, data);

            var relativePath = Path.Combine("CaseData", "PolicyDocuments", subFolder ?? "", fileName)
                .Replace("\\", "/");

            return (fileName, relativePath);
        }
        private string GetFolder(string? subFolder)
        {
            var root = Path.Combine(env.ContentRootPath, "CaseData", "PolicyDocuments");

            if (!string.IsNullOrWhiteSpace(subFolder))
                root = Path.Combine(root, subFolder);

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            return root;
        }
    }
}
