using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    public class DistrictController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public DistrictController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: District
        public async Task<IActionResult> Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("District")]
        public async Task<IActionResult> Profile()
        {
            var applicationDbContext = await _context.District.Include(d => d.Country).Include(d => d.State).ToListAsync();

            return View(applicationDbContext);
        }

        // GET: District/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0 || _context.District == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }

            var district = await _context.District
                .Include(d => d.Country)
                .Include(d => d.State)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }

            return View(district);
        }

        // GET: District/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: District/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(District district)
        {
            if (district is not null)
            {
                district.Updated = DateTime.UtcNow;
                district.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(district);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("district created successfully!");
                return RedirectToAction(nameof(Index));
            }
            toastNotification.AddErrorToastMessage("district not found!");
            return Problem();
        }

        // GET: District/Edit/5
        [Breadcrumb("Edit", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0 || _context.District == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }

            var district = await _context.District.FindAsync(id);
            if (district == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", district.CountryId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", district.StateId);
            return View(district);
        }

        // POST: District/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, District district)
        {
            if (id != district.DistrictId)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }

            if (district is not null)
            {
                try
                {
                    district.Updated = DateTime.UtcNow;
                    district.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(district);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DistrictExists(district.DistrictId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("district edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            toastNotification.AddErrorToastMessage("Error to edit district!");
            return Problem();
        }

        // GET: District/Delete/5
        [Breadcrumb("Delete", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.District == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }

            var district = await _context.District
                .Include(d => d.Country)
                .Include(d => d.State)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return NotFound();
            }

            return View(district);
        }

        // POST: District/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.District == null)
            {
                toastNotification.AddErrorToastMessage("district not found!");
                return Problem("Entity set 'ApplicationDbContext.District'  is null.");
            }
            var district = await _context.District.FindAsync(id);
            if (district != null)
            {
                district.Updated = DateTime.UtcNow;
                district.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.District.Remove(district);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("district deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool DistrictExists(long id)
        {
            return (_context.District?.Any(e => e.DistrictId == id)).GetValueOrDefault();
        }
    }
}