using System.Globalization;
using System.Net;

using AspNetCoreHero.ToastNotification.Abstractions;

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
    public class CaseEnablerController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        INotyfService notifyService,
        ILogger<CaseEnablerController> logger) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly ApplicationDbContext _context = context;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<CaseEnablerController> _logger = logger;
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("Reason To Verify ")]
        public IActionResult Profile()
        {
            return View();
        }

        public async Task<IActionResult> GetCaseEnablers()
        {
            var data = await _context.CaseEnabler
                .Select(c => new
                {
                    CaseEnablerId = _encryptionService.ProtectId(c.CaseEnablerId),
                    c.Name,
                    c.Code,
                    Updated = c.Updated ?? c.Created,
                    UpdateBy = c.UpdatedBy
                }).OrderByDescending(o => o.Updated)
                .ToListAsync();

            return Json(new { data });
        }

        [Breadcrumb("Add  New", FromAction = nameof(Profile))]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseEnabler caseEnabler)
        {
            if (!ModelState.IsValid)
            {
                _notifyService.Error("Reason Empty!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                caseEnabler.Code = WebUtility.HtmlEncode(caseEnabler.Code!.ToUpper(CultureInfo.InvariantCulture));
                bool exists = await _context.CaseEnabler.AnyAsync(x => x.Code == caseEnabler.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Reason Code already exists.");
                    _notifyService.Error("Reason Code already exists!");
                    return View(caseEnabler);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                caseEnabler.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(caseEnabler.Name.ToLower()));
                caseEnabler.Updated = DateTime.UtcNow;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;

                _context.Add(caseEnabler);
                await _context.SaveChangesAsync(null, false);
                _notifyService.Success($"Reason Code <b>{caseEnabler.Code}</b> created successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error to create Reason!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CheckDuplicateCode(string code, [ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long? id)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(false);

            code = code.ToUpper(CultureInfo.InvariantCulture);

            // ✅ Check if any other record (not this one) already has the same code
            bool exists = await _context.CaseEnabler.AnyAsync(x => x.Code.ToUpper() == code && (!id.HasValue || x.CaseEnablerId != id.Value));

            return Json(exists);
        }

        [Breadcrumb("Edit ", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id < 1)
            {
                _notifyService.Error("Reason Not found!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var caseEnabler = await _context.CaseEnabler.FindAsync(id);
                if (caseEnabler == null)
                {
                    _notifyService.Error("Reason Not found!");
                    return RedirectToAction(nameof(Profile));
                }
                return View(caseEnabler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error in REASON!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, CaseEnabler caseEnabler)
        {
            if (id < 1 || !ModelState.IsValid)
            {
                _notifyService.Error("Reason Null!");
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                // Uppercase normalization
                caseEnabler.Code = WebUtility.HtmlEncode(caseEnabler.Code!.ToUpper(CultureInfo.InvariantCulture));

                // Check for duplicate code before saving
                bool exists = await _context.CaseEnabler.AnyAsync(x => x.CaseEnablerId != id && x.Code == caseEnabler.Code);
                if (exists)
                {
                    ModelState.AddModelError("Code", "Reason Code already exists.");
                    _notifyService.Error("Reason Code already exists!");
                    return View(caseEnabler);
                }
                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                caseEnabler.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(caseEnabler.Name.ToLower()));

                caseEnabler.Updated = DateTime.UtcNow;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Update(caseEnabler);
                await _context.SaveChangesAsync(null, false);
                _notifyService.Custom($"Reason Code <b>{caseEnabler.Code}</b> edited successfully!", 3, "orange", "fas fa-puzzle-piece");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error editing Reason!");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
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

                caseEnabler.Updated = DateTime.UtcNow;
                caseEnabler.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.CaseEnabler.Remove(caseEnabler);
                await _context.SaveChangesAsync(null, false);
                return Json(new { success = true, message = "Reason deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred.");
                _notifyService.Error("Error deleting REASON!");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}