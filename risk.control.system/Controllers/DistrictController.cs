using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("District")]
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
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? currentPage, int pageSize = 10)
        {
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.StateSortParm = string.IsNullOrEmpty(sortOrder) ? "state_desc" : "";
            ViewBag.CountrySortParm = string.IsNullOrEmpty(sortOrder) ? "country_desc" : "";
            if (searchString != null)
            {
                currentPage = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var applicationDbContext = _context.District.Include(d => d.Country).Include(d => d.State).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                applicationDbContext = applicationDbContext.Where(a =>
                a.Code.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.State.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.Country.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.Name.ToLower().Contains(searchString.Trim().ToLower()));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.Name);
                    break;

                case "state_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.State.Name);
                    break;

                case "country_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.Country.Name);
                    break;

                default:
                    applicationDbContext.OrderByDescending(s => s.State.Name);
                    break;
            }
            int pageNumber = (currentPage ?? 1);
            ViewBag.TotalPages = (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.ShowPrevious = pageNumber > 1;
            ViewBag.ShowNext = pageNumber < (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));
            ViewBag.ShowFirst = pageNumber != 1;
            ViewBag.ShowLast = pageNumber != (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));

            var applicationDbContextResult = await applicationDbContext.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return View(applicationDbContextResult);
        }

        // GET: District/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(string id)
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

        // GET: District/Create
        [Breadcrumb("Add District")]
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
        [Breadcrumb("Edit District")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.District == null)
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
        public async Task<IActionResult> Edit(string id, District district)
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
        [Breadcrumb("Delete")]
        public async Task<IActionResult> Delete(string id)
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
        public async Task<IActionResult> DeleteConfirmed(string id)
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

        private bool DistrictExists(string id)
        {
            return (_context.District?.Any(e => e.DistrictId == id)).GetValueOrDefault();
        }
    }
}