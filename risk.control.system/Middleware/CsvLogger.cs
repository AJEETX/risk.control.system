using System.Text;
namespace risk.control.system.Middleware
{
    public class CsvLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logDirectory;
        private readonly LogLevel _minLogLevel;

        public CsvLogger(string categoryName, string logDirectory, LogLevel minLogLevel)
        {
            _categoryName = categoryName;
            _logDirectory = logDirectory;
            _minLogLevel = minLogLevel;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

        public IDisposable? BeginScope<TState>(TState state) => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                                Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string filePath = GetLogFilePath();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var message = formatter(state, exception)?.Replace("\"", "\"\"");
            var exceptionDetails = exception?.ToString().Replace("\"", "\"\"") ?? "";

            var logLine = $"\"{timestamp}\",\"{logLevel}\",\"{_categoryName}\",\"{message}\",\"{exceptionDetails}\"\n";
            File.AppendAllText(filePath, logLine, Encoding.UTF8);
        }

        private string GetLogFilePath()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = $"log_{date}.csv";
            var fullPath = Path.Combine(_logDirectory, fileName);

            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(_logDirectory);
                File.AppendAllText(fullPath, "Timestamp,Level,Category,Message,Exception\n");
            }

            return fullPath;
        }
    }

    public class CsvLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly LogLevel _minLogLevel;

        public CsvLoggerProvider(string logDirectory, LogLevel minLogLevel = LogLevel.Error)
        {
            _logDirectory = logDirectory;
            _minLogLevel = minLogLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CsvLogger(categoryName, _logDirectory, _minLogLevel);
        }

        public void Dispose() { }
    }

    public static class LogCleanup
    {
        public static void DeleteOldLogFiles(string logDirectory, int maxAgeInDays)
        {
            if (!Directory.Exists(logDirectory)) return;

            var files = Directory.GetFiles(logDirectory, "log_*.csv");
            var cutoffDate = DateTime.Now.AddDays(-maxAgeInDays);

            foreach (var file in files)
            {
                try
                {
                    var creationDate = File.GetCreationTime(file);
                    if (creationDate < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    // Optionally log cleanup failure to a fallback logger
                    Console.WriteLine($"Failed to delete log file {file}: {ex.Message}");
                }
            }
        }
    }
}
