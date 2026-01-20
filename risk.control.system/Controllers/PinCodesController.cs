using System.Linq.Expressions;
using System.Text.RegularExpressions;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class PinCodesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<PinCodesController> logger;

        public PinCodesController(ApplicationDbContext context, INotyfService notifyService, ILogger<PinCodesController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
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

            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
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
            var rawData = await query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    p.Code,
                    p.Name,
                    District = p.District.Name,
                    State = p.State.Name,
                    Country = p.Country.Name,
                    p.Updated,
                    p.PinCodeId
                })
                .ToListAsync();

            var data = rawData.Select(s => new
            {
                s.Code,
                s.Name,
                District = s.District,
                State = s.State,
                Country = s.Country,
                s.PinCodeId,
                Updated = s.Updated?.ToString("dd-MMM-yyyy HH:mm"),
            });
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
                notifyService.Error("Pincode not found!");
                return RedirectToAction(nameof(Profile));
            }

            var pinCode = await _context.PinCode
                .Include(p => p.Country)
                .Include(p => p.State)
                .Include(p => p.District)
                .FirstOrDefaultAsync(m => m.PinCodeId == id);
            if (pinCode == null)
            {
                notifyService.Error("Pincode not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(pinCode);
        }

        // GET: PinCodes/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var user = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

            var district = new PinCode { IsUpdated = !user.IsSuperAdmin, Country = user.Country, CountryId = user.CountryId.GetValueOrDefault(), SelectedCountryId = user.CountryId.GetValueOrDefault() };
            return View(district);
        }

        // POST: PinCodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PinCode pinCode)
        {
            if (pinCode is null)
            {
                notifyService.Error("Pincode Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                pinCode.Updated = DateTime.Now;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                pinCode.CountryId = pinCode.SelectedCountryId;
                pinCode.StateId = pinCode.SelectedStateId;
                pinCode.DistrictId = pinCode.SelectedDistrictId;
                _context.Add(pinCode);
                await _context.SaveChangesAsync();
                notifyService.Success("Pincode created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("Error to create Pincode!");
                return RedirectToAction(nameof(Profile));
            }
        }

        // GET: PinCodes/Edit/5
        [Breadcrumb("Edit  ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("Pincode not found!");
                return RedirectToAction(nameof(Profile));
            }

            var pinCode = await _context.PinCode.Include(d => d.Country).Include(d => d.State).Include(d => d.District).FirstOrDefaultAsync(p => p.PinCodeId == id);
            if (pinCode == null)
            {
                notifyService.Error("Pincode not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(pinCode);
        }

        // POST: PinCodes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, PinCode pinCode)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("Pincode Null!");
                    return RedirectToAction(nameof(Profile));
                }
                var existingPincode = await _context.PinCode.FindAsync(id);
                existingPincode.Code = pinCode.Code;
                existingPincode.Name = pinCode.Name;
                existingPincode.Updated = DateTime.Now;
                existingPincode.UpdatedBy = HttpContext.User?.Identity?.Name;
                existingPincode.CountryId = pinCode.SelectedCountryId;
                existingPincode.StateId = pinCode.SelectedStateId;
                existingPincode.DistrictId = pinCode.SelectedDistrictId;
                _context.Update(existingPincode);
                if (await _context.SaveChangesAsync() > 0)
                {
                    notifyService.Custom($"Pincode edited successfully!", 3, "orange", "far fa-edit");
                    return RedirectToAction(nameof(Profile));
                }
                else
                {
                    notifyService.Error("An error occurred while updating the pincode!");
                    return RedirectToAction(nameof(Profile));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("An error occurred while updating the pincode!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id < 1)
            {
                return Json(new { success = true, message = "Pincode not found!" });
            }
            try
            {
                var pinCode = await _context.PinCode.FindAsync(id);
                if (pinCode == null)
                {
                    return Json(new { success = true, message = "Pincode not found!" });
                }
                pinCode.Updated = DateTime.Now;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.PinCode.Remove(pinCode);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Pincode deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("Error to delete District!");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}