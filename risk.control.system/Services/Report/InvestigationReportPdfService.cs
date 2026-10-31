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
            var pdfReportFilePath = await GetPdfFilePath(id);

            var fileName = Path.GetFileName(pdfReportFilePath);
            var fullFilePath = Path.GetFullPath(pdfReportFilePath!);
            var memory = new MemoryStream();
            await using var stream = new FileStream(fullFilePath!, FileMode.Open);
            await stream.CopyToAsync(memory);
            memory.Position = 0;
            return (fileName!, memory!);
        }

        private async Task<string?> GetPdfFilePath(long id)
        {
            var investigation = await _context.Investigations.Include(i => i.InvestigationReport).AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            return investigation!.InvestigationReport!.PdfReportFilePath;
        }
    }
}