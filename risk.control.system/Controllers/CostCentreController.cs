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
    public class CostCentreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public CostCentreController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        // GET: CostCentre
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Budget Centre")]
        public async Task<IActionResult> Profile()
        {
            return _context.CostCentre != null ?
                        View(await _context.CostCentre.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.CostCentre'  is null.");
        }

        // GET: CostCentre/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.CostCentre == null)
            {
                return NotFound();
            }

            var costCentre = await _context.CostCentre
                .FirstOrDefaultAsync(m => m.CostCentreId == id);
            if (costCentre == null)
            {
                return NotFound();
            }

            return View(costCentre);
        }

        // GET: CostCentre/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: CostCentre/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CostCentre costCentre)
        {
            if (costCentre is not null)
            {
                costCentre.Updated = DateTime.Now;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(costCentre);
                await _context.SaveChangesAsync();
                notifyService.Success("cost centre created successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(costCentre);
        }

        // GET: CostCentre/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var costCentre = await _context.CostCentre.FindAsync(id);
            if (costCentre == null)
            {
                return NotFound();
            }
            return View(costCentre);
        }

        // POST: CostCentre/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("CostCentreId,Name,Code,Created,Updated,UpdatedBy")] CostCentre costCentre)
        {
            if (id != costCentre.CostCentreId)
            {
                return NotFound();
            }

            if (costCentre is not null)
            {
                try
                {
                    costCentre.Updated = DateTime.Now;
                    costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(costCentre);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CostCentreExists(costCentre.CostCentreId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                notifyService.Success("cost centre edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(costCentre);
        }

        // POST: CostCentre/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.CostCentre == null)
            {
                return Problem("Entity set 'ApplicationDbContext.CostCentre'  is null.");
            }
            var costCentre = await _context.CostCentre.FindAsync(id);
            if (costCentre != null)
            {
                costCentre.Updated = DateTime.Now;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CostCentre.Remove(costCentre);
            }

            await _context.SaveChangesAsync();
            notifyService.Success("cost centre deleted successfully!");
            return Json(new { success = true, message = "Cost centre deleted successfully!" });
        }

        private bool CostCentreExists(long id)
        {
            return (_context.CostCentre?.Any(e => e.CostCentreId == id)).GetValueOrDefault();
        }
    }
}