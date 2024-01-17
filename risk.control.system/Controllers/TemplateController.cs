using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class TemplateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TemplateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Template
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var applicationDbContext = _context.ReportTemplate
                .Include(r => r.ClientCompany)
                .Include(r => r.DigitalIdReport)
                .Include(r => r.ReportQuestionaire)
                .Include(r => r.DocumentIdReport).Where(c => c.ClientCompanyId == user.ClientCompanyId);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Template/Details/5
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0 || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate
                .Include(r => r.DigitalIdReport)
                .Include(r => r.DocumentIdReport)
                .Include(r => r.ReportQuestionaire)
                .FirstOrDefaultAsync(m => m.ReportTemplateId == id);
            if (reportTemplate == null)
            {
                return NotFound();
            }

            return View(reportTemplate);
        }

        // GET: Template/Create
        public IActionResult Create()
        {
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "ReportType");
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportType");
            ViewData["ReportQuestionaireId"] = new SelectList(_context.ReportQuestionaire, "ReportQuestionaireId", "Question");
            return View();
        }

        // POST: Template/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReportTemplateId,Name,DigitalIdReportId,DocumentIdReportId,ReportQuestionaireId,Created,Updated,UpdatedBy")] ReportTemplate reportTemplate)
        {
            if (ModelState.IsValid)
            {
                var userEmail = HttpContext?.User?.Identity?.Name;
                reportTemplate.Updated = DateTime.UtcNow;
                reportTemplate.UpdatedBy = userEmail;
                var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                reportTemplate.ClientCompanyId = user.ClientCompanyId;
                _context.Add(reportTemplate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "ReportType", reportTemplate.DigitalIdReportId);
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportType", reportTemplate.DocumentIdReportId);
            ViewData["ReportQuestionaireId"] = new SelectList(_context.ReportQuestionaire, "ReportQuestionaireId", "Question", reportTemplate.ReportQuestionaireId);
            return View(reportTemplate);
        }

        // GET: Template/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0 || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate.FindAsync(id);
            if (reportTemplate == null)
            {
                return NotFound();
            }
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "ReportType", reportTemplate.DigitalIdReportId);
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportType", reportTemplate.DocumentIdReportId);
            ViewData["ReportQuestionaireId"] = new SelectList(_context.ReportQuestionaire, "ReportQuestionaireId", "Question", reportTemplate.ReportQuestionaireId);
            return View(reportTemplate);
        }

        // POST: Template/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("ReportTemplateId,Name,DigitalIdReportId,DocumentIdReportId,ReportQuestionaireId,Created,Updated,UpdatedBy")] ReportTemplate reportTemplate)
        {
            if (id != reportTemplate.ReportTemplateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userEmail = HttpContext?.User?.Identity?.Name;
                    reportTemplate.Updated = DateTime.UtcNow;
                    reportTemplate.UpdatedBy = userEmail;
                    var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                    reportTemplate.ClientCompanyId = user.ClientCompanyId;
                    _context.Update(reportTemplate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReportTemplateExists(reportTemplate.ReportTemplateId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "ReportType", reportTemplate.DigitalIdReportId);
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportType", reportTemplate.DocumentIdReportId);
            ViewData["ReportQuestionaireId"] = new SelectList(_context.ReportQuestionaire, "ReportQuestionaireId", "Question", reportTemplate.ReportQuestionaireId);
            return View(reportTemplate);
        }

        // GET: Template/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate
                .Include(r => r.DigitalIdReport)
                .Include(r => r.DocumentIdReport)
                .Include(r => r.ReportQuestionaire)
                .FirstOrDefaultAsync(m => m.ReportTemplateId == id);
            if (reportTemplate == null)
            {
                return NotFound();
            }

            return View(reportTemplate);
        }

        // POST: Template/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.ReportTemplate == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ReportTemplate'  is null.");
            }
            var reportTemplate = await _context.ReportTemplate.FindAsync(id);
            if (reportTemplate != null)
            {
                _context.ReportTemplate.Remove(reportTemplate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReportTemplateExists(long id)
        {
            return (_context.ReportTemplate?.Any(e => e.ReportTemplateId == id)).GetValueOrDefault();
        }
    }
}