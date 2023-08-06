using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Pincode")]
    public class PinCodesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public PinCodesController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: PinCodes
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? currentPage, int pageSize = 10)
        {
            ViewBag.CodeSortParm = string.IsNullOrEmpty(sortOrder) ? "code_desc" : "";
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DistrictSortParm = string.IsNullOrEmpty(sortOrder) ? "district_desc" : "";
            ViewBag.StateSortParm = string.IsNullOrEmpty(sortOrder) ? "state_desc" : "";
            if (searchString != null)
            {
                currentPage = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var applicationDbContext = _context.PinCode.Include(p => p.Country).Include(p => p.District).Include(p => p.State).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                applicationDbContext = applicationDbContext.Where(a =>
                a.Code.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.District.Name.ToLower().Contains(searchString.Trim().ToLower()) ||
                a.Name.ToLower().Contains(searchString.Trim().ToLower()));
            }

            switch (sortOrder)
            {
                case "code_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.Code);
                    break;

                case "name_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.Name);
                    break;

                case "district_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.District.Name);
                    break;

                case "state_desc":
                    applicationDbContext = applicationDbContext.OrderByDescending(s => s.State.Name);
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
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            return View(applicationDbContextResult);
        }

        // GET: PinCodes/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            var pinCode = await _context.PinCode
                .Include(p => p.Country)
                .Include(p => p.State)
                .Include(p => p.District)
                .FirstOrDefaultAsync(m => m.PinCodeId == id);
            if (pinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            return View(pinCode);
        }

        // GET: PinCodes/Create
        [Breadcrumb("Add Pincode")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: PinCodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PinCode pinCode)
        {
            pinCode.Updated = DateTime.UtcNow;
            pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;

            _context.Add(pinCode);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("pincode created successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: PinCodes/Edit/5
        [Breadcrumb("Edit Pincode")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            var pinCode = await _context.PinCode.FindAsync(id);
            if (pinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", pinCode.CountryId);
            ViewData["StateId"] = new SelectList(_context.State.Where(s => s.CountryId == pinCode.CountryId), "StateId", "Name", pinCode?.StateId);
            ViewData["DistrictId"] = new SelectList(_context.District.Where(s => s.StateId == pinCode.StateId), "DistrictId", "Name", pinCode?.DistrictId);
            return View(pinCode);
        }

        // POST: PinCodes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, PinCode pinCode)
        {
            if (id != pinCode.PinCodeId)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }
            try
            {
                pinCode.Updated = DateTime.UtcNow;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(pinCode);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PinCodeExists(pinCode.PinCodeId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            toastNotification.AddSuccessToastMessage("pincode edited successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: PinCodes/Delete/5
        [Breadcrumb("Delete Pincode")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            var pinCode = await _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District)
                .FirstOrDefaultAsync(m => m.PinCodeId == id);
            if (pinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }

            return View(pinCode);
        }

        // POST: PinCodes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return Problem("Entity set 'ApplicationDbContext.PinCode'  is null.");
            }
            var pinCode = await _context.PinCode.FindAsync(id);
            if (pinCode != null)
            {
                pinCode.Updated = DateTime.UtcNow;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.PinCode.Remove(pinCode);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("pincode deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool PinCodeExists(string id)
        {
            return (_context.PinCode?.Any(e => e.PinCodeId == id)).GetValueOrDefault();
        }
    }
}