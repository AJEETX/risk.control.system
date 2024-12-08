using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class InvestigationServiceTypesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public InvestigationServiceTypesController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: InvestigationServiceTypes
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Type Of Service ")]
        public async Task<IActionResult> Profile()
        {
            var applicationDbContext = _context.InvestigationServiceType.Include(i => i.LineOfBusiness);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: InvestigationServiceTypes/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.InvestigationServiceType == null)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return NotFound();
            }

            var investigationServiceType = await _context.InvestigationServiceType
                .Include(i => i.LineOfBusiness)
                .FirstOrDefaultAsync(m => m.InvestigationServiceTypeId == id);
            if (investigationServiceType == null)
            {
                return NotFound();
            }

            return View(investigationServiceType);
        }

        // GET: InvestigationServiceTypes/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View();
        }

        // POST: InvestigationServiceTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvestigationServiceType investigationServiceType)
        {
            if (investigationServiceType is not null)
            {
                investigationServiceType.Updated = DateTime.Now;
                investigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(investigationServiceType);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("service type created successfully!");
                return RedirectToAction(nameof(Index));
            }
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", investigationServiceType.LineOfBusinessId);
            toastNotification.AddErrorToastMessage("Error to create service type!");
            return View(investigationServiceType);
        }

        // GET: InvestigationServiceTypes/Edit/5
        [Breadcrumb("Edit", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1 || _context.InvestigationServiceType == null)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return NotFound();
            }

            var investigationServiceType = await _context.InvestigationServiceType.FindAsync(id);
            if (investigationServiceType == null)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return NotFound();
            }
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", investigationServiceType.LineOfBusinessId);
            return View(investigationServiceType);
        }

        // POST: InvestigationServiceTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, InvestigationServiceType investigationServiceType)
        {
            if (id != investigationServiceType.InvestigationServiceTypeId)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return NotFound();
            }

            if (investigationServiceType is not null)
            {
                try
                {
                    investigationServiceType.Updated = DateTime.Now;
                    investigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;

                    _context.Update(investigationServiceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvestigationServiceTypeExists(investigationServiceType.InvestigationServiceTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("service type edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", investigationServiceType.LineOfBusinessId);
            toastNotification.AddErrorToastMessage("Error to edit service type!");
            return View(investigationServiceType);
        }

        // GET: InvestigationServiceTypes/Delete/5
        [Breadcrumb("Delete", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.InvestigationServiceType == null)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return NotFound();
            }

            var investigationServiceType = await _context.InvestigationServiceType
                .Include(i => i.LineOfBusiness)
                .FirstOrDefaultAsync(m => m.InvestigationServiceTypeId == id);
            if (investigationServiceType == null)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return NotFound();
            }

            return View(investigationServiceType);
        }

        // POST: InvestigationServiceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.InvestigationServiceType == null)
            {
                toastNotification.AddErrorToastMessage("service type not found!");
                return Problem("Entity set 'ApplicationDbContext.InvestigationServiceType'  is null.");
            }
            var investigationServiceType = await _context.InvestigationServiceType.FindAsync(id);
            if (investigationServiceType != null)
            {
                investigationServiceType.Updated = DateTime.Now;
                investigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;

                _context.InvestigationServiceType.Remove(investigationServiceType);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("service type deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool InvestigationServiceTypeExists(long id)
        {
            return (_context.InvestigationServiceType?.Any(e => e.InvestigationServiceTypeId == id)).GetValueOrDefault();
        }
    }
}