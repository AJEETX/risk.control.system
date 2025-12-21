using System.Globalization;
using System.Net;

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
    public class CaseEnablerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<CaseEnablerController> logger;

        public CaseEnablerController(ApplicationDbContext context, INotyfService notifyService, ILogger<CaseEnablerController> logger)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Reason To Verify ")]
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult GetCaseEnablers()
        {
            var data = _context.CaseEnabler
                .Select(c => new
                {
                    c.CaseEnablerId,
                    c.Name,
                    c.Code,
                    Updated = c.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm"),
                    UpdateBy = c.UpdatedBy
                }).ToList();

            return Json(new { data });
        }
        [Breadcrumb("Details ")]
        public async Task<IActionResult> Details(int id)
        {
            if (id < 1 || _context.CaseEnabler == null)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var caseEnabler = await _context.CaseEnabler.FirstOrDefaultAsync(m => m.CaseEnablerId == id);
                if (caseEnabler == null)
                {
                    notifyService.Error("Reason Not found!");
                    return RedirectToAction(nameof(Profile));
                }
                return View(caseEnabler);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error to get Reason !");
                return RedirectToAction(nameof(Profile));
            }
        }

        [Breadcrumb("Add  New", FromAction = "Profile")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseEnabler caseEnabler)
        {
            if (caseEnabler is null || !ModelState.IsValid)
            {
                notifyService.Error("Reason Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                caseEnabler.Code = WebUtility.HtmlEncode(caseEnabler.Code?.ToUpper(CultureInfo.InvariantCulture));
                bool exists = await _context.CaseEnabler.AnyAsync(x => x.Code == caseEnabler.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Reason Code already exists.");
                    notifyService.Error("Reason Code already exists!");
                    return View(caseEnabler);
                }
                caseEnabler.Name = WebUtility.HtmlEncode(caseEnabler.Name);
                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;

                _context.Add(caseEnabler);
                await _context.SaveChangesAsync();
                notifyService.Success("Reason created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error to create Reason!");
                return RedirectToAction(nameof(Profile));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, long? id)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            code = code.ToUpper(CultureInfo.InvariantCulture);

            // ✅ Check if any other record (not this one) already has the same code
            bool exists = await _context.CaseEnabler.AnyAsync(x => x.Code.ToUpper() == code && (!id.HasValue || x.CaseEnablerId != id.Value));

            return Json(exists);
        }

        [Breadcrumb("Edit ", FromAction = "Profile")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var caseEnabler = await _context.CaseEnabler.FindAsync(id);
                if (caseEnabler == null)
                {
                    notifyService.Error("Reason Not found!");
                    return RedirectToAction(nameof(Profile));
                }
                return View(caseEnabler);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error in REASON!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, CaseEnabler caseEnabler)
        {
            if (id < 1 || !ModelState.IsValid)
            {
                notifyService.Error("Reason Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                // Uppercase normalization
                caseEnabler.Code = WebUtility.HtmlEncode(caseEnabler.Code?.ToUpper(CultureInfo.InvariantCulture));
                caseEnabler.Name = WebUtility.HtmlEncode(caseEnabler.Name);
                // Check for duplicate code before saving
                bool exists = await _context.CaseEnabler.AnyAsync(x => x.CaseEnablerId != id && x.Code == caseEnabler.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Reason Code already exists.");
                    notifyService.Error("Reason Code already exists!");
                    return View(caseEnabler);
                }

                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(caseEnabler);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Reason edited successfully!", 3, "orange", "fas fa-puzzle-piece");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error editing Reason!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Reason Not found!" });
            }
            try
            {
                var caseEnabler = await _context.CaseEnabler.FindAsync(id);
                if (caseEnabler == null)
                {
                    return Json(new { success = false, message = "Reason Not found!" });
                }

                caseEnabler.Updated = DateTime.Now;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CaseEnabler.Remove(caseEnabler);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Reason deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred.");
                notifyService.Error("Error deleting REASON!");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}