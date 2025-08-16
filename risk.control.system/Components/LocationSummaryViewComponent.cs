using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;

namespace risk.control.system.Components
{
    public class LocationSummaryViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public LocationSummaryViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(long reportTemplateId)
        {
            var template = await _context.ReportTemplates
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationTemplate)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(q => q.Id == reportTemplateId);
            return View(template.LocationTemplate); // This will look for Views/Shared/Components/Location/Default.cshtml
        }
    }
}
