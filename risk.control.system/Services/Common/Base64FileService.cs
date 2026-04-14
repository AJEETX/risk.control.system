namespace risk.control.system.Services.Common
{
    public interface IBase64FileService
    {
        Task<string> GetBase64FileAsync(string relativePath, string fallback = "");

        Task<byte[]> GetByteFileAsync(string relativePath);
    }

    internal class Base64FileService(IWebHostEnvironment env) : IBase64FileService
    {
        private readonly IWebHostEnvironment _env = env;

        public async Task<string> GetBase64FileAsync(string relativePath, string fallback = "")
        {
            if (string.IsNullOrEmpty(relativePath)) return fallback;

            var fullPath = Path.Combine(_env.ContentRootPath, relativePath);
            if (!File.Exists(fullPath)) return fallback;

            // Use the async version of file reading
            byte[] bytes = await File.ReadAllBytesAsync(fullPath);
            return $"data:image/*;base64,{Convert.ToBase64String(bytes)}";
        }

        public async Task<byte[]> GetByteFileAsync(string relativePath)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, relativePath);

            byte[] bytes = await File.ReadAllBytesAsync(fullPath);
            return bytes;
        }
    }
}