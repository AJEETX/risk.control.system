using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;

namespace risk.control.system.Components
{
    public class DecisionViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public DecisionViewComponent(ApplicationDbContext context)
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
                .FirstOrDefaultAsync(q => q.Id == reportTemplateId);
            return View(template.LocationTemplate); // This will look for Views/Shared/Components/Location/Default.cshtml
        }
    }
}
