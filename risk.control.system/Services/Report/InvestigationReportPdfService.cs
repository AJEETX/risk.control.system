using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IInvestigationReportPdfService
    {
        Task<(string, MemoryStream)> PrintPdfReport(string currentUserEmail, long id);
    }

    internal class InvestigationReportPdfService(ApplicationDbContext context) : IInvestigationReportPdfService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<(string, MemoryStream)> PrintPdfReport(string currentUserEmail, long id)
        {
            // Try to get the path with a built-in polling retry (e.g., up to 30 seconds total)
            var pdfReportFilePath = await GetPdfFilePathWithRetryAsync(id, maxRetries: 10, delaySeconds: 3);

            if (string.IsNullOrEmpty(pdfReportFilePath))
            {
                throw new InvalidOperationException("The PDF report is still being generated or failed to process. Please try again shortly.");
            }

            var fileName = Path.GetFileName(pdfReportFilePath);
            var fullFilePath = Path.GetFullPath(pdfReportFilePath);

            if (!File.Exists(fullFilePath))
            {
                throw new FileNotFoundException("The generated PDF file could not be found on disk.", fullFilePath);
            }

            var memory = new MemoryStream();
            await using var stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await stream.CopyToAsync(memory);
            memory.Position = 0;

            return (fileName, memory);
        }

        private async Task<string?> GetPdfFilePathWithRetryAsync(long id, int maxRetries, int delaySeconds)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // 1. Query fresh from the database on every attempt to catch background updates
                var investigation = await _context.Investigations
                    .Include(i => i.InvestigationReport)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                var path = investigation?.InvestigationReport?.PdfReportFilePath;

                // 2. If path is available and file actually exists on disk, return it
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(Path.GetFullPath(path)))
                {
                    return path;
                }

                // 3. Wait before the next retry attempt
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            return null; // All retries exhausted
        }
    }
}