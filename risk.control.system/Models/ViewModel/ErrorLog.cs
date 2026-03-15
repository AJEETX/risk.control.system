namespace risk.control.system.Models.ViewModel
{
    public class LogFileViewModel
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public DateTime Date { get; set; }
        public long SizeKB { get; set; }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string MessageTemplate { get; set; }
        public Exception Exception { get; set; }
        public string RenderedMessage { get; set; }
    }
}