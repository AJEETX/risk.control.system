using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Creator
{
    public interface IReportTemplateService
    {
        Task<ReportTemplate> GetReportTemplate(long caseId);
    }

    internal class ReportTemplateService : IReportTemplateService
    {
        private readonly ApplicationDbContext _context;

        public ReportTemplateService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<ReportTemplate> GetReportTemplate(long caseId)
        {
            var claimsInvestigation = await _context.Investigations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == caseId);

            var template = await _context.ReportTemplates
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(r => r.Id == claimsInvestigation.ReportTemplateId);

            return template;
        }
    }
}