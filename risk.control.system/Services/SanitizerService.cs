
using Ganss.Xss;

namespace risk.control.system.Services
{
    public interface ISanitizerService
    {
        string Sanitize(string input);
    }

    // Services/SanitizerService.cs
    public class SanitizerService : ISanitizerService
    {
        private readonly HtmlSanitizer _sanitizer = new();
        public string Sanitize(string input) => input == null ? string.Empty : _sanitizer.Sanitize(input);
    }
}
