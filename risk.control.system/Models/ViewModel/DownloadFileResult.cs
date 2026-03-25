namespace risk.control.system.Models.ViewModel
{
    public class DownloadFileResult
    {
        public Stream FileStream { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = "application/zip";
        public string ErrorMessage { get; set; } = default!;
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }

    public class DownloadErrorFileResult
    {
        public byte[] FileBytes { get; set; } = default!;// Added for in-memory data
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public string ErrorMessage { get; set; } = default!;
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}