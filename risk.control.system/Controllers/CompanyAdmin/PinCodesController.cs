using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
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
            return RedirectToAction(nameof(Profile));
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
            var userEmail = HttpContext.User.Identity?.Name!;

            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (!user!.IsSuperAdmin)
            {
                query = query.Where(s => s.CountryId == user.CountryId);
            }
            // Filter based on the search input (if provided)
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p =>
                    p.Code.ToString().Contains(lowerSearch) ||
                    p.Name.ToLower().Contains(lowerSearch) ||
                    p.District!.Name.ToLower().Contains(lowerSearch) ||
                    p.State!.Name.ToLower().Contains(lowerSearch) ||
                    p.Country!.Name.ToLower().Contains(lowerSearch));
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
                    District = p.District!.Name,
                    State = p.State!.Name,
                    Country = p.Country!.Name,
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

        [HttpGet]
        public async Task<IActionResult> GetDistrictsByStatesAndCountry(long countryId, long stateId)
        {
            var states = await _context.District
                .Where(s => s.CountryId == countryId && s.StateId == stateId)
                .Select(s => new { id = s.DistrictId, name = s.Name })
                .ToListAsync();

            return Json(states);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, long? id, long DistrictId, long StateId, long CountryId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            bool exists = await _context.PinCode.AnyAsync(x => x.CountryId == CountryId && x.StateId == StateId && x.DistrictId == DistrictId && x.Code.ToString() == code && (!id.HasValue || x.DistrictId != id.Value));

            return Json(exists);
        }

        [Breadcrumb("Add New", FromAction = nameof(Profile))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User.Identity?.Name!;

            var user = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

            var pincode = new PinCode
            {
                IsUpdated = !user!.IsSuperAdmin,
                Country = user.Country,
                CountryId = user.CountryId.GetValueOrDefault(),
                SelectedCountryId = user.CountryId.GetValueOrDefault()
            };
            return View(pincode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PinCode pinCode)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Custom("Invalid Pincode data!", 3, "red", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var pinCodeExist = await _context.PinCode.AnyAsync(p => p.Code == pinCode.Code && p.CountryId == pinCode.SelectedCountryId);
                if (pinCodeExist)
                {
                    notifyService.Custom($"Pincode <b>{pinCode.Code}</b> already exist in Country!", 3, "red", "fas fa-map-pin");
                    ModelState.Clear();

                    var userEmail = HttpContext.User.Identity?.Name!;

                    var user = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == userEmail);
                    return View(new PinCode
                    {
                        IsUpdated = !user!.IsSuperAdmin,
                        Country = user.Country,
                        CountryId = user.CountryId.GetValueOrDefault(),
                        SelectedCountryId = user.CountryId.GetValueOrDefault(),
                        StateId = pinCode.StateId,
                        SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                        DistrictId = pinCode.DistrictId,
                        SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault()
                    });
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                pinCode.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(pinCode.Name.ToLower()));
                pinCode.Updated = DateTime.UtcNow;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                pinCode.CountryId = pinCode.SelectedCountryId;
                _context.Add(pinCode);
                await _context.SaveChangesAsync(null, false);
                notifyService.Custom($"Pincode <b>{pinCode.Name} ({pinCode.Code})</b> created successfully!", 3, "green", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Custom("An error occurred while creating the Pincode!", 3, "red", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }
        }

        [Breadcrumb("Edit  ", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit(long id)
        {
            if (id <= 0)
            {
                notifyService.Custom("Pincode Not Found!", 3, "red", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }

            var pinCode = await _context.PinCode.Include(d => d.Country).Include(d => d.State).Include(d => d.District).FirstOrDefaultAsync(p => p.PinCodeId == id);
            if (pinCode == null)
            {
                notifyService.Custom("Pincode Not Found!", 3, "red", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }

            return View(pinCode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, PinCode pinCode)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom("Invalid Pincode data!", 3, "red", "fas fa-map-pin");
                    return RedirectToAction(nameof(Profile));
                }
                var existingPincode = await _context.PinCode.FindAsync(id);
                if (existingPincode == null)
                {
                    notifyService.Custom("Pincode Not Found!", 3, "red", "fas fa-map-pin");
                    return RedirectToAction(nameof(Profile));
                }
                var pinCodeExist = await _context.PinCode.AnyAsync(p => p.Code == pinCode.Code && p.CountryId == pinCode.SelectedCountryId && p.PinCodeId != pinCode.PinCodeId);
                if (pinCodeExist)
                {
                    notifyService.Custom($"Pincode <b>{pinCode.Code}</b> already exist in Country!", 3, "red", "fas fa-map-pin");
                    var currentPinCode = await _context.PinCode.Include(d => d.Country).Include(d => d.State).Include(d => d.District).FirstOrDefaultAsync(p => p.PinCodeId == id);

                    return View(currentPinCode);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                existingPincode.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(pinCode.Name.ToLower()));
                existingPincode.Code = pinCode.Code;
                existingPincode.Updated = DateTime.UtcNow;
                existingPincode.UpdatedBy = HttpContext.User?.Identity?.Name;
                existingPincode.CountryId = pinCode.SelectedCountryId;
                existingPincode.StateId = pinCode.SelectedStateId;
                _context.Update(existingPincode);
                await _context.SaveChangesAsync(null, false);
                notifyService.Custom($"Pincode <b>{existingPincode.Name} ({existingPincode.Code})</b> edited successfully!", 3, "orange", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Custom("An error occurred while updating the Pincode!", 3, "red", "fas fa-map-pin");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = true, message = "Pincode Not Found!" });
            }
            try
            {
                var pinCode = await _context.PinCode.FindAsync(id);
                if (pinCode == null)
                {
                    return Json(new { success = true, message = "Pincode Not Found!" });
                }
                pinCode.Updated = DateTime.UtcNow;
                pinCode.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.PinCode.Remove(pinCode);
                await _context.SaveChangesAsync(null, false);
                return Json(new { success = true, message = "Pincode deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                return Json(new { success = false, message = "An error occurred while updating the Pincode!" });
            }
        }
    }
}