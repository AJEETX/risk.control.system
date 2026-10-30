using System.Globalization;
using System.Net;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CostCentreController(ApplicationDbContext context, INotyfService notifyService, ILogger<CostCentreController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<CostCentreController> _logger = logger;

        // GET: CostCentre
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("Budget Centre")]
        public IActionResult Profile()
        {
            return View();
        }

        public async Task<IActionResult> GetCostCentres()
        {
            var data = await _context.CostCentre
                .Select(c => new
                {
                    c.CostCentreId,
                    c.Name,
                    c.Code,
                    Updated = c.Updated ?? c.Created,
                    UpdateBy = c.UpdatedBy
                }).OrderByDescending(o => o.Updated)
                .ToListAsync();

            return Json(new { data });
        }

        // GET: CostCentre/Create
        [Breadcrumb("Add New", FromAction = nameof(Profile))]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CostCentre costCentre)
        {
            if (!ModelState.IsValid)
            {
                _notifyService.Error("Budget Centre  Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                costCentre.Code = WebUtility.HtmlEncode(costCentre.Code!.ToUpper(CultureInfo.InvariantCulture));

                // Check for duplicate code before saving
                bool exists = await _context.CostCentre
                    .AnyAsync(x => x.Code == costCentre.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Budget Centre Code already exists.");
                    _notifyService.Error("Budget Centre Code already exists!");
                    return View(costCentre);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                costCentre.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(costCentre.Name.ToLower()));

                costCentre.Updated = DateTime.UtcNow;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Add(costCentre);
                await _context.SaveChangesAsync(null, false);
                _notifyService.Success($"Budget Centre Code <b>{costCentre.Code}</b> created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Budget Centre.");
                _notifyService.Error("Error creating Budget Centre. Try again.");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, long? id)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            bool exists = await _context.CostCentre.AnyAsync(x => x.Code.ToUpper() == code.ToUpper(CultureInfo.InvariantCulture) && (!id.HasValue || x.CostCentreId != id.Value));

            return Json(exists);
        }

        // GET: CostCentre/Edit/5
        [Breadcrumb("Edit ", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit(long id)
        {
            if (id <= 0)
            {
                _notifyService.Error("Budget Centre Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var costCentre = await _context.CostCentre.FindAsync(id);
            if (costCentre == null)
            {
                _notifyService.Error("Budget Centre Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(costCentre);
        }

        // POST: CostCentre/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("CostCentreId,Name,Code,Created,Updated,UpdatedBy")] CostCentre costCentre)
        {
            if (id < 1 || !ModelState.IsValid)
            {
                _notifyService.Error("Budget Centre Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                // Uppercase normalization
                costCentre.Code = WebUtility.HtmlEncode(costCentre.Code!.ToUpper(CultureInfo.InvariantCulture));

                // Check for duplicate code before saving
                bool exists = await _context.CostCentre.AnyAsync(x => x.Code == costCentre.Code && x.CostCentreId != id);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Budget Centre Code already exists.");
                    _notifyService.Error("Budget Centre Code already exists!");
                    return View(costCentre);
                }

                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                costCentre.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(costCentre.Name.ToLower()));

                costCentre.Updated = DateTime.UtcNow;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(costCentre);
                await _context.SaveChangesAsync(null, false);
                _notifyService.Custom($"Budget Centre Code <b>{costCentre.Code}</b> edited successfully!", 3, "orange", "far fa-edit");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Budget Centre.");
                _notifyService.Error("Error editing Budget Centre. Try again.");
                return RedirectToAction(nameof(Profile));
            }
        }

        // POST: CostCentre/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Budget Centre Not found!" });
            }
            try
            {
                var costCentre = await _context.CostCentre.FindAsync(id);
                if (costCentre == null)
                {
                    return Json(new { success = false, message = "Budget Centre Not found!" });
                }
                costCentre.Updated = DateTime.UtcNow;
                costCentre.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CostCentre.Remove(costCentre);
                await _context.SaveChangesAsync(null, false);
                return Json(new { success = true, message = "Budget Centre deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Budget Centre. ");
                _notifyService.Error("Error deleting Budget Centre. Try again.");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}