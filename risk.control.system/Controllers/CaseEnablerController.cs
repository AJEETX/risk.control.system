using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CaseEnablerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public CaseEnablerController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Reason To Verify ")]
        public async Task<IActionResult> Profile()
        {
            return _context.CaseEnabler != null ?
                        View(await _context.CaseEnabler.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.CaseEnabler'  is null.");
        }

        // GET: CaseEnabler/Details/5
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(int id)
        {
            if (id < 1 || _context.CaseEnabler == null)
            {
                return NotFound();
            }

            var caseEnabler = await _context.CaseEnabler
                .FirstOrDefaultAsync(m => m.CaseEnablerId == id);
            if (caseEnabler == null)
            {
                return NotFound();
            }

            return View(caseEnabler);
        }

        // GET: CaseEnabler/Create
        [Breadcrumb("Add  New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: CaseEnabler/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseEnabler caseEnabler)
        {
            if (caseEnabler is not null)
            {
                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(caseEnabler);
                await _context.SaveChangesAsync();
                notifyService.Success("case enabler created successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(caseEnabler);
        }

        // GET: CaseEnabler/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1 || _context.CaseEnabler == null)
            {
                return NotFound();
            }

            var caseEnabler = await _context.CaseEnabler.FindAsync(id);
            if (caseEnabler == null)
            {
                return NotFound();
            }
            return View(caseEnabler);
        }

        // POST: CaseEnabler/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, CaseEnabler caseEnabler)
        {
            if (id != caseEnabler.CaseEnablerId)
            {
                return NotFound();
            }

            if (caseEnabler is not null)
            {
                try
                {
                    caseEnabler.Updated = DateTime.Now;
                    caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(caseEnabler);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CaseEnablerExists(caseEnabler.CaseEnablerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                notifyService.Success("case enabler edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(caseEnabler);
        }

        // POST: CaseEnabler/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.CaseEnabler == null)
            {
                return Problem("Entity set 'ApplicationDbContext.CaseEnabler'  is null.");
            }
            var caseEnabler = await _context.CaseEnabler.FindAsync(id);
            if (caseEnabler != null)
            {
                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CaseEnabler.Remove(caseEnabler);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Case enabler deleted successfully!" });
        }

        private bool CaseEnablerExists(long id)
        {
            return (_context.CaseEnabler?.Any(e => e.CaseEnablerId == id)).GetValueOrDefault();
        }
    }
}