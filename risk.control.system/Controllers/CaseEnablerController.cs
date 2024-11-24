using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    public class CaseEnablerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public CaseEnablerController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
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
                toastNotification.AddSuccessToastMessage("case enabler created successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(caseEnabler);
        }

        // GET: CaseEnabler/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1  || _context.CaseEnabler == null)
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
                toastNotification.AddSuccessToastMessage("case enabler edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(caseEnabler);
        }

        // GET: CaseEnabler/Delete/5
        [Breadcrumb("Delete ", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
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
            toastNotification.AddSuccessToastMessage("case enabler deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool CaseEnablerExists(long id)
        {
            return (_context.CaseEnabler?.Any(e => e.CaseEnablerId == id)).GetValueOrDefault();
        }
    }
}