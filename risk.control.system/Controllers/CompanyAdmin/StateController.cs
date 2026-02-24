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

        // GET: RiskCaseStatus
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
            var userEmail = HttpContext.User.Identity.Name;

            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (!user.IsSuperAdmin)
            {
                query = query.Where(s => s.CountryId == user.CountryId);
            }
            // Apply search filter
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                search = search.Trim().Replace("%", "[%]")
                   .Replace("_", "[_]")
                   .Replace("[", "[[]");
                query = query.Where(p =>
                    EF.Functions.Like(p.Code, $"%{search}%") ||
                    EF.Functions.Like(p.Name, $"%{search}%") ||
                    EF.Functions.Like(p.Country.Name, $"%{search}%"));
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
                s.Updated,
                CountryName = s.Country.Name
            })
            .ToListAsync();
            // Apply paging
            // Now format the datetime in memory
            var data = rawData.Select(s => new
            {
                s.StateId,
                s.Name,
                s.Code,
                Updated = s.Updated?.ToString("dd-MMM-yyyy HH:mm"),
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

        // GET: RiskCaseStatus/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id < 1)
            {
                notifyService.Error("State not found!");
                return RedirectToAction(nameof(Profile));
            }

            var state = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                notifyService.Error("State not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(state);
        }

        [Breadcrumb("Add New", FromAction = nameof(Profile))]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var user = await _context.ApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user.IsSuperAdmin)
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
            if (state is null)
            {
                notifyService.Error("Invalid State data!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                state.Code = WebUtility.HtmlEncode(state.Code?.ToUpper(CultureInfo.InvariantCulture));
                bool exists = await _context.State.AnyAsync(x => x.Code == state.Code && x.CountryId == state.SelectedCountryId);
                if (exists)
                {
                    notifyService.Error("State Code already exists!");
                    return RedirectToAction(nameof(Profile));
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                state.Name = textInfo.ToTitleCase(state.Name.ToLower());
                state.Updated = DateTime.UtcNow;
                state.CountryId = state.SelectedCountryId;
                state.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.State.Add(state);
                await _context.SaveChangesAsync();
                notifyService.Success("State created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("Error to create State!");
                return RedirectToAction(nameof(Profile));
            }
        }

        // GET: RiskCaseStatus/Edit/5
        [Breadcrumb("Edit", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Error("State not found!");
                return RedirectToAction(nameof(Profile));
            }

            var state = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
            if (state == null)
            {
                notifyService.Error("State not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(state);
        }

        // POST: RiskCaseStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, State state)
        {
            if (id < 1 && state is null)
            {
                notifyService.Error("State Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                bool exists = await _context.State.AnyAsync(x => x.Code == state.Code && x.CountryId == state.SelectedCountryId && x.StateId != id);
                if (exists)
                {
                    notifyService.Error("State Code already exists!");
                    return RedirectToAction(nameof(Profile));
                }
                var existingState = _context.State.Find(id);
                existingState.Code = state.Code;
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                existingState.Name = textInfo.ToTitleCase(state.Name.ToLower());
                existingState.Updated = DateTime.UtcNow;
                existingState.CountryId = state.SelectedCountryId;
                existingState.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(existingState);
                await _context.SaveChangesAsync();
                notifyService.Custom($"State edited successfully!", 3, "orange", "far fa-edit");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("Error to edit State!");
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: RiskCaseStatus/Delete/5
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
                    return Json(new { success = false, message = "State not found!" });
                }
                var hasDistrict = _context.District.Any(d => d.StateId == id);
                if (hasDistrict)
                {
                    return Json(new { success = false, message = $"Cannot delete State {state.Name}. It has associated districts." });
                }
                state.Updated = DateTime.UtcNow;
                state.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.State.Remove(state);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "State deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("Error to delete State!");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}