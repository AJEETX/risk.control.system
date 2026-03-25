namespace risk.control.system.Models.ViewModel
{
    public class LogFileViewModel
    {
        public string FileName { get; set; } = default!;
        public string FullPath { get; set; } = default!;
        public DateTime Date { get; set; }
        public long SizeKB { get; set; }
    }
}