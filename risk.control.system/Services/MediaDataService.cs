using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;

namespace risk.control.system.Services
{
    public interface IMediaDataService
    {
        Task SaveTranscript(long locationId, string reportName, string transcript);
    }
    public class MediaDataService : IMediaDataService
    {
        private readonly ApplicationDbContext context;

        public MediaDataService(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task SaveTranscript(long locationId, string reportName, string transcript)
        {
            var locationTemplate = await context.LocationReport
               .Include(l => l.MediaReports)
               .FirstOrDefaultAsync(l => l.Id == locationId);

            // Save to DB
            var media = locationTemplate.MediaReports.FirstOrDefault(c => c.ReportName == reportName);

            media.Transcript = transcript;

            await context.SaveChangesAsync();
        }
    }
}
