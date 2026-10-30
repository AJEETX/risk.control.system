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
    public class OccupationTypeController(ApplicationDbContext context, INotyfService notifyService, ILogger<OccupationTypeController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<OccupationTypeController> _logger = logger;

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("Occupation Type")]
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult GetOccupations()
        {
            var data = _context.OccupationType
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")
                }).ToList();

            return Json(new { data });
        }

        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(int id)
        {
            if (id < 1)
            {
                _notifyService.Error("Occupation Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var income = await _context.OccupationType
                .FirstOrDefaultAsync(m => m.Id == id);
            if (income == null)
            {
                _notifyService.Error("Occupation Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(income);
        }

        [Breadcrumb("Add  New", FromAction = nameof(Profile))]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OccupationType occupation)
        {
            if (occupation is not null)
            {
                occupation.Updated = DateTime.UtcNow;
                occupation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.OccupationType.Add(occupation);
                await _context.SaveChangesAsync();
                _notifyService.Success("Occupation Type created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            return View(occupation);
        }

        [Breadcrumb("Edit ", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                _notifyService.Error("Occupation Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var occupation = await _context.OccupationType.FindAsync(id);
            if (occupation == null)
            {
                _notifyService.Error("Occupation Type Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(occupation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, OccupationType occupation)
        {
            if (id != occupation.Id)
            {
                _notifyService.Error("Occupation Type Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                occupation.Updated = DateTime.UtcNow;
                occupation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.OccupationType.Update(occupation);
                await _context.SaveChangesAsync();
                _notifyService.Warning("Occupation Type edited successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Occupation Type");
                _notifyService.Error("Error editing Occupation Type. Try again.");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Occupation Type Not found!" });
            }
            var occupation = await _context.OccupationType.FindAsync(id);
            if (occupation != null)
            {
                occupation.Updated = DateTime.UtcNow;
                occupation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.OccupationType.Remove(occupation);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Occupation Type deleted successfully!" });
        }
    }
}