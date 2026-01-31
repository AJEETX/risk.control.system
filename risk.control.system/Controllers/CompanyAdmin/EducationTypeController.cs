using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.AppConstant;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class EducationTypeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<EducationTypeController> logger;

        public EducationTypeController(ApplicationDbContext context, INotyfService notifyService, ILogger<EducationTypeController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Education Type")]
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult GetEducations()
        {
            var data = _context.EducationType
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
                notifyService.Error("Education Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var income = await _context.EducationType
                .FirstOrDefaultAsync(m => m.Id == id);
            if (income == null)
            {
                notifyService.Error("Education Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            return View(income);
        }

        [Breadcrumb("Add  New", FromAction = "Profile")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EducationType education)
        {
            if (education is not null)
            {
                education.Updated = DateTime.Now;
                education.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.EducationType.Add(education);
                await _context.SaveChangesAsync();
                notifyService.Success("Education Type created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            return View(education);
        }

        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Education Type Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var education = await _context.EducationType.FindAsync(id);
            if (education == null)
            {
                notifyService.Error("Education Type Not found!");
                return RedirectToAction(nameof(Profile));
            }
            return View(education);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EducationType education)
        {
            if (id != education.Id)
            {
                notifyService.Error("Education Type Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                education.Updated = DateTime.Now;
                education.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.EducationType.Update(education);
                await _context.SaveChangesAsync();
                notifyService.Warning("Education Type edited successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing Education Type");
                notifyService.Error("Error editing Education Type. Try again.");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Education Type Not found!" });
            }
            var education = await _context.EducationType.FindAsync(id);
            if (education != null)
            {
                education.Updated = DateTime.Now;
                education.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.EducationType.Remove(education);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Education Type deleted successfully!" });
        }
    }
}
