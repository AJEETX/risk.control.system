using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Components
{
    public class LocationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public LocationViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(long reportTemplateId, long caseId, InsuranceType insuranceType)
        {
            var template = await _context.ReportTemplates
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationTemplate)
                .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(q => q.Id == reportTemplateId);

            if (template != null)
            {
                template.CaseId = caseId;
                template.InsuranceType = insuranceType;
            }

            return View(template); // This will look for Views/Shared/Components/Location/Default.cshtml
        }
    }
}
