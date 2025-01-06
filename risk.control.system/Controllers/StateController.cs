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
    public class StateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public StateController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: RiskCaseStatus
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
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

            var user = _context.ApplicationUser.FirstOrDefault(u => u.Email == userEmail);
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

            // Apply paging
            var data = await query
                .Skip(start)
                .Take(length)
                .Select(s => new
                {
                    s.StateId,
                    s.Name,
                    s.Code,
                    CountryName = s.Country.Name
                })
                .ToListAsync();

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
            if (id < 1 || _context.State == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            var state = await _context.State.Include(s => s.Country)
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            return View(state);
        }

        [Breadcrumb("Add New", FromAction ="Profile")]
        public IActionResult Create()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var user = _context.ApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            if (user.IsSuperAdmin)
            {
                return View();
            }
            var state = new State { CountryId = user.CountryId.GetValueOrDefault(), SelectedCountryId = user.CountryId.GetValueOrDefault() };
            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(State state)
        {
            state.Updated = DateTime.Now;
            state.CountryId = state.SelectedCountryId;
            state.UpdatedBy = HttpContext.User?.Identity?.Name;
            _context.Add(state);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("state created successfully!");
            return RedirectToAction(nameof(Index));
        }

        // GET: RiskCaseStatus/Edit/5
        [Breadcrumb("Edit", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1 || _context.State == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            var state = await _context.State.Include(s => s.Country).FirstOrDefaultAsync(c => c.StateId == id);
            if (state == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }
            //ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", state.CountryId);

            return View(state);
        }

        // POST: RiskCaseStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, State state)
        {
            if (id != state.StateId)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            if (state is not null)
            {
                try
                {
                    state.Updated = DateTime.Now;
                    state.CountryId = state.SelectedCountryId;
                    state.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(state);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                toastNotification.AddSuccessToastMessage("state edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            //ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            toastNotification.AddErrorToastMessage("Error to edit state!");
            return View(state);
        }

        // GET: RiskCaseStatus/Delete/5
        [Breadcrumb("Delete", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id < 1  || _context.State == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            var state = await _context.State
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                toastNotification.AddErrorToastMessage("state not found!");
                return NotFound();
            }

            return View(state);
        }

        // POST: RiskCaseStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.State == null)
            {
                return Problem("Entity set 'ApplicationDbContext.State'  is null.");
            }
            var state = await _context.State.FindAsync(id);
            if (state != null)
            {
                state.Updated = DateTime.Now;
                state.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.State.Remove(state);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("state deleted successfully!");
            return RedirectToAction(nameof(Index));
        }

        private bool StateExists(long id)
        {
            return (_context.State?.Any(e => e.StateId == id)).GetValueOrDefault();
        }
    }
}