using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class AuditController(
        ApplicationDbContext context,
        IEncryptionService encryptionService
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IEncryptionService _encryptionService = encryptionService;

        // GET: Audit
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("Audit Log")]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet] // keep it simple
        public async Task<IActionResult> GetAudit(int draw, int start, int length, string search, int? orderColumn, string orderDirection)
        {
            var userEmail = HttpContext.User.Identity?.Name!;
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var query = _context.AuditLogs.Where(a => !string.IsNullOrWhiteSpace(a.NewValues) && !string.IsNullOrWhiteSpace(a.UserId) && a.CompanyId == companyUser!.ClientCompanyId && a.TableName != "StatusNotification").AsQueryable();
            int recordsTotal = await query.CountAsync();
            if (!string.IsNullOrEmpty(search) && Regex.IsMatch(search, @"^[a-zA-Z0-9\s]*$"))
            {
                search = search.Trim().Replace("%", "[%]").Replace("_", "[_]").Replace("[", "[[]");
                query = query.Where(p => EF.Functions.Like(p.UserId, $"%{search}%") || EF.Functions.Like(p.TableName, $"%{search}%") || EF.Functions.Like(p.Type, $"%{search}%") || EF.Functions.Like(p.OldValues, $"%{search}%") || EF.Functions.Like(p.NewValues, $"%{search}%"));
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
            bool isAscending = orderDirection?.ToLower() == "asc";
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
            query = isAscending ? query.OrderBy(lambda) : query.OrderByDescending(lambda);
            int totalRecords = await query.CountAsync();
            var data = await query.Skip(start).Take(length).Select(p => new
            {
                Id = _encryptionService.ProtectId(p.Id),
                p.UserId,
                p.TableName,
                p.Type,
                DateTime = p.DateTime,
                OldValues = p.OldValues ?? "",
                p.NewValues
            }).ToListAsync();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            });
        }

        // GET: Audit/Details/5
        [Breadcrumb("Detail ", FromAction = nameof(Profile))]
        public async Task<IActionResult> Detail([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] int id)
        {
            if (id <= 0)
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
    }
}