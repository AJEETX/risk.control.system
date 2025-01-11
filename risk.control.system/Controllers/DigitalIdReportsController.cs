using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class DigitalIdReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DigitalIdReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DigitalIdReports
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var model = await _context.DigitalIdReport
                .Include(d => d.ClientCompany)
                .Where(c => c.ClientCompanyId == user.ClientCompanyId)?.ToListAsync();

            return View(model);
        }

        // GET: DigitalIdReports/Details/5
        public async Task<IActionResult> Details(long id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var digitalIdReport = await _context.DigitalIdReport
                .FirstOrDefaultAsync(m => m.DigitalIdReportId == id);
            if (digitalIdReport == null)
            {
                return NotFound();
            }

            return View(digitalIdReport);
        }

        // GET: DigitalIdReports/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DigitalIdReports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DigitalIdReportId,DigitalIdImagePath,DigitalIdImage,DigitalIdImageData,DigitalIdImageLocationUrl,DigitalIdImageLocationAddress,DigitalIdImageMatchConfidence,DigitalIdImageLongLat,DigitalIdImageLongLatTime,ReportType,Created,Updated,UpdatedBy")] DigitalIdReport digitalIdReport)
        {
            if (ModelState.IsValid)
            {
                var userEmail = HttpContext?.User?.Identity?.Name;
                var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                digitalIdReport.ClientCompanyId = user.ClientCompanyId;
                digitalIdReport.Updated = DateTime.Now;
                digitalIdReport.UpdatedBy = userEmail;

                _context.Add(digitalIdReport);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(digitalIdReport);
        }

        // GET: DigitalIdReports/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.DigitalIdReport == null)
            {
                return NotFound();
            }

            var digitalIdReport = await _context.DigitalIdReport.FindAsync(id);
            if (digitalIdReport == null)
            {
                return NotFound();
            }
            return View(digitalIdReport);
        }

        // POST: DigitalIdReports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("DigitalIdReportId,DigitalIdImagePath,DigitalIdImage,DigitalIdImageData,DigitalIdImageLocationUrl,DigitalIdImageLocationAddress,DigitalIdImageMatchConfidence,DigitalIdImageLongLat,DigitalIdImageLongLatTime,ReportType,Created,Updated,UpdatedBy")] DigitalIdReport digitalIdReport)
        {
            if (id != digitalIdReport.DigitalIdReportId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userEmail = HttpContext?.User?.Identity?.Name;
                    var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                    digitalIdReport.ClientCompanyId = user.ClientCompanyId;
                    digitalIdReport.Updated = DateTime.Now;
                    digitalIdReport.UpdatedBy = userEmail;
                    _context.Update(digitalIdReport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DigitalIdReportExists(digitalIdReport.DigitalIdReportId))
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
            return View(digitalIdReport);
        }

        // GET: DigitalIdReports/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.DigitalIdReport == null)
            {
                return NotFound();
            }

            var digitalIdReport = await _context.DigitalIdReport
                .FirstOrDefaultAsync(m => m.DigitalIdReportId == id);
            if (digitalIdReport == null)
            {
                return NotFound();
            }

            return View(digitalIdReport);
        }

        // POST: DigitalIdReports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.DigitalIdReport == null)
            {
                return Problem("Entity set 'ApplicationDbContext.DigitalIdReport'  is null.");
            }
            var digitalIdReport = await _context.DigitalIdReport.FindAsync(id);
            if (digitalIdReport != null)
            {
                _context.DigitalIdReport.Remove(digitalIdReport);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DigitalIdReportExists(long id)
        {
            return (_context.DigitalIdReport?.Any(e => e.DigitalIdReportId == id)).GetValueOrDefault();
        }
    }
}