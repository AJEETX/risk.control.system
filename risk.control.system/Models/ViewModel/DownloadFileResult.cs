namespace risk.control.system.Models.ViewModel
{
    public class DownloadFileResult
    {
        public Stream FileStream { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; } = "application/zip";
        public string ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }

    public class DownloadErrorFileResult
    {
        public Stream FileStream { get; set; }
        public byte[] FileBytes { get; set; } // Added for in-memory data
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}