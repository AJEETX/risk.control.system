using System.Linq.Expressions;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.AppConstant;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Audit
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }
        [Breadcrumb("Audit Log")]
        public IActionResult Profile()
        {
            return View();
        }
        [HttpGet] // keep it simple
        public async Task<IActionResult> GetAudit(int draw, int start, int length, string search, int? orderColumn, string orderDirection)
        {
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var query = _context.AuditLogs.Where(a => !string.IsNullOrWhiteSpace(a.NewValues) &&
            !string.IsNullOrWhiteSpace(a.UserId) &&
            a.CompanyId == companyUser.ClientCompanyId &&
            a.TableName != "StatusNotification").AsQueryable();

            int recordsTotal = await query.CountAsync();

            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                search = search.Trim().Replace("%", "[%]")
                   .Replace("_", "[_]")
                   .Replace("[", "[[]");
                query = query.Where(p =>
                    EF.Functions.Like(p.UserId, $"%{search}%") ||
                    EF.Functions.Like(p.TableName, $"%{search}%") ||
                    EF.Functions.Like(p.Type, $"%{search}%") ||
                    EF.Functions.Like(p.OldValues, $"%{search}%") ||
                    EF.Functions.Like(p.NewValues, $"%{search}%"));
            }
            string sortColumn = orderColumn switch
            {
                0 => "UserId",          // First column (index 0) - Code
                1 => "TableName",          // Second column (index 1) - Name
                2 => "Type",    // Third column (index 2) - State
                3 => "DateTime",  // Fourth column (index 3) - Country
                4 => "OldValues",  // Fourth column (index 3) - Country
                5 => "NewValues",  // Fourth column (index 3) - Country
                _ => "DateTime"           // Default to "Code" if no column is specified
            };

            // Determine sort direction
            bool isAscending = orderDirection?.ToLower() == "asc";

            // Dynamically apply sorting using reflection
            var parameter = Expression.Parameter(typeof(Audit), "p");
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

            var lambda = Expression.Lambda<Func<Audit, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);

            // Apply sorting
            query = isAscending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);
            int totalRecords = await query.CountAsync();

            var rawData = await query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    p.TableName,
                    p.Type,
                    p.DateTime,
                    p.OldValues,
                    p.NewValues
                })
                .ToListAsync();

            var data = rawData.Select(p => new
            {
                p.Id,
                p.UserId,
                p.TableName,
                p.Type,
                DateTime = p.DateTime.ToString("dd-MMM-yyyy HH:mm"),
                p.OldValues,
                p.NewValues
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

        // GET: Audit/Details/5
        [Breadcrumb("Detail ", FromAction = "Profile")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.AuditLogs == null)
            {
                return NotFound();
            }

            var audit = await _context.AuditLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (audit == null)
            {
                return NotFound();
            }

            return View(audit);
        }

        // GET: Audit/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Audit/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Type,TableName,DateTime,OldValues,NewValues,AffectedColumns,PrimaryKey")] Audit audit)
        {
            if (ModelState.IsValid)
            {
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(audit);
        }

        // GET: Audit/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.AuditLogs == null)
            {
                return NotFound();
            }

            var audit = await _context.AuditLogs.FindAsync(id);
            if (audit == null)
            {
                return NotFound();
            }
            return View(audit);
        }

        // POST: Audit/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,UserId,Type,TableName,DateTime,OldValues,NewValues,AffectedColumns,PrimaryKey")] Audit audit)
        {
            if (id != audit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(audit);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuditExists(audit.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(audit);
        }

        // GET: Audit/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.AuditLogs == null)
            {
                return NotFound();
            }

            var audit = await _context.AuditLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (audit == null)
            {
                return NotFound();
            }

            return View(audit);
        }

        // POST: Audit/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.AuditLogs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.AuditLogs'  is null.");
            }
            var audit = await _context.AuditLogs.FindAsync(id);
            if (audit != null)
            {
                _context.AuditLogs.Remove(audit);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AuditExists(long id)
        {
            return (_context.AuditLogs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
