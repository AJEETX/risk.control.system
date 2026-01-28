namespace risk.control.system.Services
{
    public interface IBase64FileService
    {
        Task<string> GetBase64FileAsync(string relativePath, string fallback = "");
    }
    internal class Base64FileService: IBase64FileService
    {
        private readonly IWebHostEnvironment env;

        public Base64FileService(IWebHostEnvironment env)
        {
            this.env = env;
        }
        public async Task<string> GetBase64FileAsync(string relativePath, string fallback = "")
        {
            if (string.IsNullOrEmpty(relativePath)) return fallback;

            var fullPath = Path.Combine(env.ContentRootPath, relativePath);
            if (!File.Exists(fullPath)) return fallback;

            // Use the async version of file reading
            byte[] bytes = await File.ReadAllBytesAsync(fullPath);
            return $"data:image/*;base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
