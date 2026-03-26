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
    public class StateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<StateController> logger;

        public StateController(ApplicationDbContext context, INotyfService notifyService, ILogger<StateController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("State")]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStates(int draw, int start, int length, string search, int? orderColumn, string orderDirection)
        {
            // Determine column to sort by
            string sortColumn = orderColumn switch
            {
                0 => "Code",   // First column (index 0)
                1 => "Name",   // Second column (index 1)
                2 => "Country.Name",   // Third column (index 2)
                _ => "Code"    // Default to "Code" if no column is specified
            };

            // Determine sort direction
            bool isAscending = orderDirection?.ToLower() == "asc";
            var query = _context.State
                .Include(s => s.Country)
                .AsQueryable();
            var userEmail = HttpContext.User.Identity?.Name;

            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (!user!.IsSuperAdmin)
            {
                query = query.Where(s => s.CountryId == user.CountryId);
            }
            // Apply search filter
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p =>
                    p.Code.ToLower().Contains(lowerSearch) ||
                    p.Name.ToLower().Contains(lowerSearch) ||
                    p.Country!.Name.ToLower().Contains(lowerSearch));
            }

            // Dynamically apply sorting using reflection
            var parameter = Expression.Parameter(typeof(State), "p");

            // Helper method to handle nested properties (e.g., "Country.Name")
            Expression GetPropertyExpression(Expression parentExpression, string propertyName)
            {
                var property = Expression.Property(parentExpression, propertyName);
                return property;
            }

            // Handle sorting by related entity (e.g., Country.Name)
            Expression propertyExpression = parameter;

            if (sortColumn.Contains('.'))
            {
                var parts = sortColumn.Split('.');
                foreach (var part in parts)
                {
                    propertyExpression = GetPropertyExpression(propertyExpression, part); // Traverse nested properties
                }
            }
            else
            {
                propertyExpression = GetPropertyExpression(propertyExpression, sortColumn); // Simple property (e.g., Code or Name)
            }

            var lambda = Expression.Lambda<Func<State, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);

            // Apply sorting
            query = isAscending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);

            // Get total records before filtering
            var totalRecords = await query.CountAsync();
            var rawData = await query
            .Skip(start)
            .Take(length)
            .Select(s => new
            {
                s.StateId,
                s.Name,
                s.Code,
                s.UpdatedBy,
                s.Updated,
                s.Created,
                CountryName = s.Country!.Name
            })
            .ToListAsync();
            // Apply paging
            // Now format the datetime in memory
            var data = rawData.Select(s => new
            {
                s.StateId,
                s.Name,
                s.Code,
                s.UpdatedBy,
                Updated = s.Updated ?? s.Created,
                s.CountryName
            });

            // Prepare DataTables response
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            };

            return Json(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, long? id, long CountryId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            bool exists = await _context.State.AnyAsync(x => x.CountryId == CountryId && x.Code.ToUpper() == code.ToUpper(CultureInfo.InvariantCulture) && (!id.HasValue || x.StateId != id.Value));

            return Json(exists);
        }

        [Breadcrumb("Add New", FromAction = nameof(Profile))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User.Identity?.Name;

            var user = await _context.ApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user!.IsSuperAdmin)
            {
                return View();
            }
            var state = new State { Country = user.Country, CountryId = user.CountryId.GetValueOrDefault(), SelectedCountryId = user.CountryId.GetValueOrDefault() };
            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(State state)
        {
            var userEmail = HttpContext.User.Identity?.Name;
            var user = await _context.ApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(u => u.Email == userEmail);
            if (!ModelState.IsValid)
            {
                notifyService.Custom($"Invalid State data!", 3, "red", "fas fa-map-marker-alt");

                // This is the magic line that clears the cached input values
                ModelState.Clear();

                state.Country = user!.Country;
                state.CountryId = user.CountryId.GetValueOrDefault();
                state.Name = "";
                state.Code = "";
                return View(state);
            }

            try
            {
                bool exists = await _context.State.AnyAsync(x => x.Code == state.Code && x.CountryId == state.SelectedCountryId);
                if (exists)
                {
                    notifyService.Custom($"State Code <b>{state.Code}</b> already exists!", 3, "red", "fas fa-map-marker-alt");

                    // Clear ModelState here as well
                    ModelState.Clear();

                    state.Country = user!.Country;
                    state.CountryId = user.CountryId.GetValueOrDefault();
                    state.SelectedCountryId = user.CountryId.GetValueOrDefault();
                    state.Name = "";
                    state.Code = "";
                    return View(state);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                state.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(state.Name.ToLower()));
                state.Code = WebUtility.HtmlEncode(state.Code.ToUpper());
                state.Updated = DateTime.UtcNow;
                state.CountryId = state.SelectedCountryId;
                state.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.State.Add(state);
                await _context.SaveChangesAsync(null, false);
                notifyService.Custom($"State <b>{state.Name}</b> created successfully!", 3, "green", "fas fa-map-marker-alt");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Custom($"An error occurred while creating the State!!", 3, "red", "fas fa-map-marker-alt");
                return RedirectToAction(nameof(Profile));
            }
        }

        [Breadcrumb("Edit", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Custom($"State Not Found!", 3, "red", "fas fa-map-marker-alt");
                return RedirectToAction(nameof(Profile));
            }

            var state = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
            if (state == null)
            {
                notifyService.Custom($"State Not Found!", 3, "red", "fas fa-map-marker-alt");
                return RedirectToAction(nameof(Profile));
            }

            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, State state)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Custom($"Invalid State data!", 3, "red", "fas fa-map-marker-alt");
                var currentState = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
                return View(currentState);
            }
            try
            {
                var existingState = await _context.State.FindAsync(id);
                if (existingState == null)
                {
                    notifyService.Custom($"State Not Found!", 3, "red", "fas fa-map-marker-alt");
                    return RedirectToAction(nameof(Profile));
                }
                bool exists = await _context.State.AnyAsync(x => x.Code == state.Code && x.CountryId == state.SelectedCountryId && x.StateId != id);
                if (exists)
                {
                    notifyService.Custom($"State Code <b>{state.Code}</b> already exists!", 3, "red", "fas fa-map-marker-alt");
                    ModelState.Remove("Name");
                    ModelState.Remove("Code");
                    var currentState = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
                    currentState!.Name = "";
                    currentState.Code = "";
                    return View(currentState);
                }
                existingState.Code = WebUtility.HtmlEncode(state.Code.ToUpper());
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                existingState.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(state.Name.ToLower()));
                existingState.Updated = DateTime.UtcNow;
                existingState.CountryId = state.SelectedCountryId;
                existingState.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(existingState);
                await _context.SaveChangesAsync(null, false);
                notifyService.Custom($"State <b>{existingState.Name}</b> edited successfully!", 3, "orange", "fas fa-map-marker-alt");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Custom($"An error occurred while editing the State!!", 3, "red", "fas fa-map-marker-alt");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = true, message = "State Not found!" });
            }
            try
            {
                var state = await _context.State.FindAsync(id);
                if (state == null)
                {
                    return Json(new { success = false, message = "State Not Found!" });
                }
                var hasDistrict = _context.District.Any(d => d.StateId == id);
                if (hasDistrict)
                {
                    return Json(new { success = false, message = $"Cannot delete State {state.Name}. It has associated districts." });
                }
                state.Updated = DateTime.UtcNow;
                state.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.State.Remove(state);
                await _context.SaveChangesAsync(null, false);
                return Json(new { success = true, message = $"State <b>{state.Name}</b> deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                return Json(new { success = false, message = $"An error occurred while deleting the State!" });
            }
        }
    }
}