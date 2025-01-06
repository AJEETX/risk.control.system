using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
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
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Pincode")]
        public IActionResult Profile()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetPincodes(int draw, int start, int length, string search, int orderColumn, string orderDirection)
        {
            var query = _context.PinCode
                .Include(p => p.Country)
                .Include(p => p.District)
                .Include(p => p.State)
                .AsQueryable();
            var userEmail = HttpContext.User.Identity.Name;

            var user = _context.ApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            if (!user.IsSuperAdmin)
            {
                query = query.Where(s => s.CountryId == user.CountryId);
            }
            // Filter based on the search input (if provided)
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                search = search.Trim().Replace("%", "[%]")
                   .Replace("_", "[_]")
                   .Replace("[", "[[]");
                query = query.Where(p =>
                    EF.Functions.Like(p.Code, $"%{search}%") ||
                    EF.Functions.Like(p.Name, $"%{search}%") ||
                    EF.Functions.Like(p.District.Name, $"%{search}%") ||
                    EF.Functions.Like(p.State.Name, $"%{search}%") ||
                    EF.Functions.Like(p.Country.Name, $"%{search}%"));
            }
            string sortColumn = orderColumn switch
            {
                0 => "Code",       // Column index 0 - Code
                1 => "Name",       // Column index 1 - Name
                2 => "District.Name", // Column index 2 - District Name
                3 => "State.Name",    // Column index 3 - State Name
                4 => "Country.Name",  // Column index 4 - Country Name
                _ => "Code"          // Default to Code if index is invalid
            };

            // Determine sort direction
            bool isAscending = orderDirection?.ToLower() == "asc";

            // Dynamically apply sorting using reflection
            var parameter = Expression.Parameter(typeof(PinCode), "p");
            Expression propertyExpression = parameter;

            if (sortColumn.Contains('.'))
            {
                var parts = sortColumn.Split('.');
                foreach (var part in parts)
                {
                    propertyExpression = Expression.Property(propertyExpression, part);
                }
            }
            else
            {
                propertyExpression = Expression.Property(parameter, sortColumn);
            }

            var lambda = Expression.Lambda<Func<PinCode, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);

            // Apply sorting based on direction
            query = isAscending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);

            // Get the total number of records before paging
            var totalRecords = await query.CountAsync();

            // Apply paging
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    p.Code,
                    p.Name,
                    District = p.District.Name,
                    State = p.State.Name,
                    Country = p.Country.Name,
                    p.PinCodeId
                })
                .ToListAsync();

            // Prepare the DataTables response
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            };

            return Json(response);
        }

        // GET: PinCodes/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1 || _context.PinCode == null)
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
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var user = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.Email == userEmail);

            if (user.IsSuperAdmin)
            {
                return View();
            }
            var district = new PinCode { Country = user.Country, CountryId = user.CountryId.GetValueOrDefault(), SelectedCountryId = user.CountryId.GetValueOrDefault() };
            return View(district);
        }

        // POST: PinCodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PinCode pinCode)
        {
            pinCode.Updated = DateTime.Now;
            pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
            pinCode.CountryId = pinCode.SelectedCountryId;
            pinCode.StateId = pinCode.SelectedStateId;
            pinCode.DistrictId = pinCode.SelectedDistrictId;
            _context.Add(pinCode);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("pincode created successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: PinCodes/Edit/5
        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
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
        public async Task<IActionResult> Edit(long id, PinCode pinCode)
        {
            if (id != pinCode.PinCodeId)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return NotFound();
            }
            try
            {
                pinCode.Updated = DateTime.Now;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                pinCode.CountryId = pinCode.SelectedCountryId;
                pinCode.StateId = pinCode.SelectedStateId;
                pinCode.DistrictId = pinCode.SelectedDistrictId;

                _context.Update(pinCode);
                if( await _context.SaveChangesAsync() > 0)
                {
                    toastNotification.AddSuccessToastMessage("pincode edited successfully!");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
               Console.WriteLine(ex.ToString());
            }
            toastNotification.AddErrorToastMessage("An error occurred while updating the pincode!");
            return RedirectToAction(nameof(Index));
        }

        // GET: PinCodes/Delete/5
        [Breadcrumb("Delete ", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
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
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.PinCode == null)
            {
                toastNotification.AddErrorToastMessage("pincode not found!");
                return Problem("Entity set 'ApplicationDbContext.PinCode'  is null.");
            }
            var pinCode = await _context.PinCode.FindAsync(id);
            if (pinCode != null)
            {
                pinCode.Updated = DateTime.Now;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.PinCode.Remove(pinCode);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("pincode deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool PinCodeExists(long id)
        {
            return (_context.PinCode?.Any(e => e.PinCodeId == id)).GetValueOrDefault();
        }
    }
}