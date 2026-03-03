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
    public class DistrictController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<DistrictController> logger;

        public DistrictController(ApplicationDbContext context, INotyfService notifyService, ILogger<DistrictController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("District")]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatesByCountry(long countryId)
        {
            var states = await _context.State
                .Where(s => s.CountryId == countryId)
                .Select(s => new { id = s.StateId, name = s.Name }).OrderBy(x => x.name)
                .ToListAsync();

            return Json(states);
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts(int draw, int start, int length, string search, int? orderColumn, string orderDirection)
        {
            var query = _context.District
                .Include(p => p.Country)
                .Include(p => p.State)
                .AsQueryable();
            var userEmail = HttpContext.User.Identity.Name;

            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (!user.IsSuperAdmin)
            {
                query = query.Where(s => s.CountryId == user.CountryId);
            }
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                search = search.Trim().Replace("%", "[%]")
                   .Replace("_", "[_]")
                   .Replace("[", "[[]");
                query = query.Where(p =>
                    EF.Functions.Like(p.Code, $"%{search}%") ||
                    EF.Functions.Like(p.Name, $"%{search}%") ||
                    EF.Functions.Like(p.State.Name, $"%{search}%") ||
                    EF.Functions.Like(p.Country.Name, $"%{search}%"));
            }
            // Determine column to sort by
            string sortColumn = orderColumn switch
            {
                0 => "Code",          // First column (index 0) - Code
                1 => "Name",          // Second column (index 1) - Name
                2 => "State.Name",    // Third column (index 2) - State
                3 => "Country.Name",  // Fourth column (index 3) - Country
                _ => "Code"           // Default to "Code" if no column is specified
            };

            // Determine sort direction
            bool isAscending = orderDirection?.ToLower() == "asc";

            // Dynamically apply sorting using reflection
            var parameter = Expression.Parameter(typeof(District), "p");
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

            var lambda = Expression.Lambda<Func<District, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);

            // Apply sorting
            query = isAscending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);

            var totalRecords = await query.CountAsync();
            var rawData = await query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    p.DistrictId,
                    p.Code,
                    p.Name,
                    p.Updated,
                    State = p.State.Name,
                    Country = p.Country.Name
                })
                .ToListAsync();

            var data = rawData.Select(p => new
            {
                p.DistrictId,
                p.Code,
                p.Name,
                Updated = p.Updated?.ToString("dd-MMM-yyyy HH:mm"),
                State = p.State,
                Country = p.Country
            }).ToList();
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            };

            return Json(response);
        }

        [Breadcrumb("Add New", FromAction = nameof(Profile))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var user = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

            var district = new District
            {
                IsUpdated = !user.IsSuperAdmin,
                Country = user.Country,
                CountryId = user.CountryId.GetValueOrDefault(),
                SelectedCountryId = user.CountryId.GetValueOrDefault()
            };
            return View(district);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(District district)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Custom($"Invalid District data!", 3, "red", "fas fa-city");
                var userEmail = HttpContext.User.Identity.Name;

                var user = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

                return View(new District
                {
                    IsUpdated = !user.IsSuperAdmin,
                    Country = user.Country,
                    CountryId = user.CountryId.GetValueOrDefault(),
                    SelectedCountryId = user.CountryId.GetValueOrDefault()
                });
            }
            try
            {
                bool exists = await _context.District.AnyAsync(x => x.Code == district.Code && x.CountryId == district.SelectedCountryId);
                if (exists)
                {
                    notifyService.Custom($"Disitrict Code <b>{district.Code}</b> already exists!", 3, "red", "fas fa-city");
                    ModelState.Clear();

                    var userEmail = HttpContext.User.Identity.Name;
                    var user = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

                    // 2. Return the fresh object. Name and Code will now be empty in the browser.
                    return View(new District
                    {
                        IsUpdated = !user.IsSuperAdmin,
                        Country = user.Country,
                        CountryId = user.CountryId.GetValueOrDefault(),
                        SelectedCountryId = user.CountryId.GetValueOrDefault(),
                        StateId = district.StateId,
                        SelectedStateId = district.StateId.GetValueOrDefault()
                    });
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                district.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(district.Name.ToLower()));
                district.Code = WebUtility.HtmlEncode(district.Code?.ToUpper());
                district.Updated = DateTime.UtcNow;
                district.UpdatedBy = HttpContext.User?.Identity?.Name;
                district.CountryId = district.SelectedCountryId;
                _context.District.Add(district);
                await _context.SaveChangesAsync(null, false);
                notifyService.Custom($"District <b>{district.Name}</b> created successfully!", 3, "green", "fas fa-city");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Custom($"An error occurred while creating the District!", 3, "red", "fas fa-city");
                return RedirectToAction(nameof(Profile));
            }
        }

        [Breadcrumb("Edit", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Custom("District Not Found!", 3, "red", "fas fa-city");
                return RedirectToAction(nameof(Profile));
            }

            var district = await _context.District.Include(d => d.Country).Include(d => d.State).FirstOrDefaultAsync(d => d.DistrictId == id);
            if (district == null)
            {
                notifyService.Custom("District Not Found!", 3, "red", "fas fa-city");
                return RedirectToAction(nameof(Profile));
            }

            return View(district);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, District district)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom($"Invalid District data!", 3, "red", "fas fa-city");
                    return RedirectToAction(nameof(Profile));
                }
                var existingdistrict = await _context.District.FindAsync(id);
                if (existingdistrict == null)
                {
                    notifyService.Custom("District Not Found!", 3, "red", "fas fa-city");
                    return RedirectToAction(nameof(Profile));
                }
                bool exists = await _context.District.AnyAsync(x => x.Code == district.Code && x.CountryId == district.SelectedCountryId && x.StateId == district.StateId && x.DistrictId != district.DistrictId);
                if (exists)
                {
                    notifyService.Custom($"Disitrict Code <b>{district.Code}</b> already exists!", 3, "red", "fas fa-city");
                    ModelState.Remove("Name");
                    ModelState.Remove("Code");
                    var currentDistrict = await _context.District.Include(d => d.Country).Include(d => d.State).FirstOrDefaultAsync(d => d.DistrictId == id);
                    currentDistrict.Name = "";
                    currentDistrict.Code = "";
                    return View(currentDistrict);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                existingdistrict.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(district.Name.ToLower()));
                existingdistrict.Code = WebUtility.HtmlEncode(district.Code);
                existingdistrict.CountryId = district.SelectedCountryId;
                existingdistrict.Updated = DateTime.UtcNow;
                existingdistrict.UpdatedBy = HttpContext.User?.Identity?.Name;
                existingdistrict.StateId = district.StateId;
                _context.Update(existingdistrict);
                await _context.SaveChangesAsync(null, false);
                notifyService.Custom($"District <b>{existingdistrict.Name}</b> edited successfully!", 3, "orange", "fas fa-city");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Custom($"An error occurred while editing the District!", 3, "red", "fas fa-city");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id < 1)
            {
                return Json(new { success = false, message = "District Not Found!" });
            }
            try
            {
                var district = await _context.District.FindAsync(id);
                if (district is null)
                {
                    return Json(new { success = false, message = "District Not Found!" });
                }
                var hasPincode = _context.PinCode.Any(p => p.DistrictId == district.DistrictId);
                if (hasPincode)
                {
                    return Json(new { success = false, message = $"Cannot delete District {district.Name}. It has associated Pincode(s)" });
                }

                district.Updated = DateTime.UtcNow;
                district.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.District.Remove(district);
                await _context.SaveChangesAsync(null, false);
                return Json(new { success = true, message = "District deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                return Json(new { success = false, message = "An error occurred while deleting the District!" });
            }
        }
    }
}