using System.Linq.Expressions;
using System.Text.RegularExpressions;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class DistrictController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;

        public DistrictController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }

        // GET: District
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("District")]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts(int draw, int start, int length, string search, int? orderColumn, string orderDirection)
        {
            var query = _context.District
                .Include(p => p.Country)
                .Include(p => p.State)
                .AsQueryable();
            var userEmail = HttpContext.User.Identity.Name;

            var user = _context.ApplicationUser.FirstOrDefault(u => u.Email == userEmail);
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

        // GET: District/Details/5
        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0 || _context.District == null)
            {
                notifyService.Error("district not found!");
                return NotFound();
            }

            var district = await _context.District
                .Include(d => d.Country)
                .Include(d => d.State)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null)
            {
                notifyService.Error("district not found!");
                return NotFound();
            }

            return View(district);
        }

        // GET: District/Create
        [Breadcrumb("Add New", FromAction = "Profile")]
        public IActionResult Create()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var user = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.Email == userEmail);

            var district = new District { IsUpdated = !user.IsSuperAdmin, Country = user.Country, CountryId = user.CountryId.GetValueOrDefault(), SelectedCountryId = user.CountryId.GetValueOrDefault() };
            return View(district);
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
                district.Updated = DateTime.Now;
                district.UpdatedBy = HttpContext.User?.Identity?.Name;
                district.CountryId = district.SelectedCountryId;
                district.StateId = district.SelectedStateId;
                _context.Add(district);
                await _context.SaveChangesAsync();
                notifyService.Success("district created successfully!");
                return RedirectToAction(nameof(Index));
            }
            notifyService.Error("district not found!");
            return Problem();
        }

        // GET: District/Edit/5
        [Breadcrumb("Edit", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0 || _context.District == null)
            {
                notifyService.Error("district not found!");
                return NotFound();
            }

            var district = await _context.District.Include(d => d.Country).Include(d => d.State).FirstOrDefaultAsync(d => d.DistrictId == id);
            if (district == null)
            {
                notifyService.Error("district not found!");
                return NotFound();
            }

            return View(district);
        }

        // POST: District/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, District district)
        {
            try
            {
                var existingdistrict = await _context.District.FindAsync(id);
                existingdistrict.Name = district.Name;
                existingdistrict.Code = district.Code;
                existingdistrict.CountryId = district.SelectedCountryId;
                existingdistrict.Updated = DateTime.Now;
                existingdistrict.UpdatedBy = HttpContext.User?.Identity?.Name;
                existingdistrict.StateId = district.SelectedStateId;
                _context.Update(existingdistrict);
                await _context.SaveChangesAsync();
                notifyService.Success("district edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            notifyService.Error("An error occurred while updating the pincode!");
            return RedirectToAction(nameof(Index));
        }

        // GET: District/Delete/5
        [Breadcrumb("Delete", FromAction = "Profile")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id < 1)
            {
                notifyService.Error("district not found!");
                return NotFound();
            }

            var district = await _context.District
                .Include(d => d.Country)
                .Include(d => d.State)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null)
            {
                notifyService.Error("district not found!");
                return NotFound();
            }

            return View(district);
        }

        // POST: District/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.District == null)
            {
                notifyService.Error("district not found!");
                return Problem("Entity set 'ApplicationDbContext.District'  is null.");
            }
            var district = await _context.District.FindAsync(id);
            if (district != null)
            {
                district.Updated = DateTime.Now;
                district.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.District.Remove(district);
            }

            await _context.SaveChangesAsync();
            notifyService.Success("district deleted successfully!");
            return RedirectToAction(nameof(Index));
        }
    }
}