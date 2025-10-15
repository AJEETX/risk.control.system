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
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class OccupationTypeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        public OccupationTypeController(ApplicationDbContext context, INotyfService notifyService)
        {
            _context = context;
            this.notifyService = notifyService;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Occupation")]
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
                notifyService.Error("Occupation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var income = await _context.OccupationType
                .FirstOrDefaultAsync(m => m.Id == id);
            if (income == null)
            {
                notifyService.Error("Occupation Not found!");
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
        public async Task<IActionResult> Create(OccupationType occupation)
        {
            if (occupation is not null)
            {
                occupation.Updated = DateTime.Now;
                occupation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.OccupationType.Add(occupation);
                await _context.SaveChangesAsync();
                notifyService.Success("Occupation created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            return View(occupation);
        }

        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Occupation Not found!");
                return RedirectToAction(nameof(Profile));
            }

            var occupation = await _context.OccupationType.FindAsync(id);
            if (occupation == null)
            {
                notifyService.Error("Occupation Not found!");
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
                notifyService.Error("Occupation Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                occupation.Updated = DateTime.Now;
                occupation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.OccupationType.Update(occupation);
                await _context.SaveChangesAsync();
                notifyService.Warning("Occupation edited successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("Error editing Occupation!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Occupation Not found!" });
            }
            var occupation = await _context.OccupationType.FindAsync(id);
            if (occupation != null)
            {
                occupation.Updated = DateTime.Now;
                occupation.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.OccupationType.Remove(occupation);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Occupation deleted successfully!" });
        }
    }
}
