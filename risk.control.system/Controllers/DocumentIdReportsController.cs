using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class DocumentIdReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocumentIdReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DocumentIdReports
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var model = await _context.DocumentIdReport
                .Include(d => d.ClientCompany)
                .Where(c => c.ClientCompanyId == user.ClientCompanyId)?.ToListAsync();

            return View(model);
        }

        // GET: DocumentIdReports/Details/5
        public async Task<IActionResult> Details(long id)
        {
            if (id == null || _context.DocumentIdReport == null)
            {
                return NotFound();
            }

            var documentIdReport = await _context.DocumentIdReport
                .FirstOrDefaultAsync(m => m.DocumentIdReportId == id);
            if (documentIdReport == null)
            {
                return NotFound();
            }

            return View(documentIdReport);
        }

        // GET: DocumentIdReports/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DocumentIdReports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DocumentIdReportId,DocumentIdImagePath,DocumentIdImage,DocumentIdImageValid,DocumentIdImageType,DocumentIdImageData,DocumentIdImageLocationUrl,DocumentIdImageLocationAddress,DocumentIdImageLongLat,DocumentIdImageLongLatTime,DocumentIdReportType,Created,Updated,UpdatedBy")] DocumentIdReport documentIdReport)
        {
            if (ModelState.IsValid)
            {
                var userEmail = HttpContext?.User?.Identity?.Name;
                documentIdReport.Updated = DateTime.UtcNow;
                documentIdReport.UpdatedBy = userEmail;
                var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                documentIdReport.ClientCompanyId = user.ClientCompanyId;

                _context.Add(documentIdReport);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(documentIdReport);
        }

        // GET: DocumentIdReports/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.DocumentIdReport == null)
            {
                return NotFound();
            }

            var documentIdReport = await _context.DocumentIdReport.FindAsync(id);
            if (documentIdReport == null)
            {
                return NotFound();
            }
            return View(documentIdReport);
        }

        // POST: DocumentIdReports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("DocumentIdReportId,DocumentIdImagePath,DocumentIdImage,DocumentIdImageValid,DocumentIdImageType,DocumentIdImageData,DocumentIdImageLocationUrl,DocumentIdImageLocationAddress,DocumentIdImageLongLat,DocumentIdImageLongLatTime,DocumentIdReportType,Created,Updated,UpdatedBy")] DocumentIdReport documentIdReport)
        {
            if (id != documentIdReport.DocumentIdReportId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userEmail = HttpContext?.User?.Identity?.Name;
                    documentIdReport.Updated = DateTime.UtcNow;
                    documentIdReport.UpdatedBy = userEmail;
                    var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                    documentIdReport.ClientCompanyId = user.ClientCompanyId;
                    _context.Update(documentIdReport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentIdReportExists(documentIdReport.DocumentIdReportId))
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
            return View(documentIdReport);
        }

        // GET: DocumentIdReports/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.DocumentIdReport == null)
            {
                return NotFound();
            }

            var documentIdReport = await _context.DocumentIdReport
                .FirstOrDefaultAsync(m => m.DocumentIdReportId == id);
            if (documentIdReport == null)
            {
                return NotFound();
            }

            return View(documentIdReport);
        }

        // POST: DocumentIdReports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.DocumentIdReport == null)
            {
                return Problem("Entity set 'ApplicationDbContext.DocumentIdReport'  is null.");
            }
            var documentIdReport = await _context.DocumentIdReport.FindAsync(id);
            if (documentIdReport != null)
            {
                _context.DocumentIdReport.Remove(documentIdReport);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DocumentIdReportExists(long id)
        {
            return (_context.DocumentIdReport?.Any(e => e.DocumentIdReportId == id)).GetValueOrDefault();
        }
    }
}