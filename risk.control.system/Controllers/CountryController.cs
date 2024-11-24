using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    public class CountryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public CountryController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: RiskCaseStatus
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

         [Breadcrumb("Country")]
        public async Task<IActionResult> Profile()
        {
            var applicationDbContext = _context.Country.AsQueryable();

            var applicationDbContextResult = await applicationDbContext.ToListAsync();
            return View(applicationDbContextResult);
        }
        // GET: RiskCaseStatus/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0 || _context.Country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }

            var country = await _context.Country
                .FirstOrDefaultAsync(m => m.CountryId == id);
            if (country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }

            return View(country);
        }

        [Breadcrumb("Add New", FromAction ="Profile")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Country country)
        {
            country.Updated = DateTime.Now;
            country.UpdatedBy = HttpContext.User?.Identity?.Name;
            _context.Add(country);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("country added successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: RiskCaseStatus/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0 || _context.Country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }

            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == id);
            if (country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }
            return View(country);
        }

        // POST: RiskCaseStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Country country)
        {
            if (id != country.CountryId)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    country.Updated = DateTime.Now;
                    country.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(country);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CountryExists(country.CountryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("country edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            toastNotification.AddErrorToastMessage("Error to edit country!");
            return View(country);
        }

        // GET: RiskCaseStatus/Delete/5
        [Breadcrumb("Delete ", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id == 0 || _context.Country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }

            var country = await _context.Country
                .FirstOrDefaultAsync(m => m.CountryId == id);
            if (country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return NotFound();
            }

            return View(country);
        }

        // POST: RiskCaseStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.Country == null)
            {
                toastNotification.AddErrorToastMessage("country not found!");
                return Problem("Entity set 'ApplicationDbContext.Country'  is null.");
            }
            var country = await _context.Country.FindAsync(id);
            if (country != null)
            {
                country.Updated = DateTime.Now;
                country.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Country.Remove(country);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("country deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool CountryExists(long id)
        {
            return (_context.Country?.Any(e => e.CountryId == id)).GetValueOrDefault();
        }
    }
}