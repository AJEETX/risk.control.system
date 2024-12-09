using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class GlobalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;

        public GlobalController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            this.toastNotification = toastNotification;
        }

        // GET: RiskCaseStatus
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

         [Breadcrumb("Global-settings")]
        public async Task<IActionResult> Profile()
        {
            var applicationDbContext = _context.GlobalSettings.AsQueryable();

            var applicationDbContextResult = await applicationDbContext.FirstOrDefaultAsync();
            return View(applicationDbContextResult);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(long id, GlobalSettings settings)
        {
            if (id < 1)
            {
                toastNotification.AddErrorToastMessage("Global-settings not found!");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var globalSettings =await _context.GlobalSettings.FirstOrDefaultAsync(x => x.GlobalSettingsId == id);
                    globalSettings.EnableMailbox = settings.EnableMailbox;
                    globalSettings.SendSMS = settings.SendSMS;
                    globalSettings.CanChangePassword = settings.CanChangePassword;
                    globalSettings.ShowTimer = settings.ShowTimer;
                    globalSettings.ShowDetailFooter = settings.ShowDetailFooter;
                    globalSettings.EnableClaim = settings.EnableClaim;
                    globalSettings.EnableUnderwriting = settings.EnableUnderwriting;
                    globalSettings.FtpUri = settings.FtpUri;
                    globalSettings.FtpUser = settings.FtpUser;
                    globalSettings.FtpData = settings.FtpData;
                    globalSettings.AddressUri = settings.AddressUri;
                    globalSettings.AddressUriData = settings.AddressUriData;
                    globalSettings.WeatherUri = settings.WeatherUri;
                    globalSettings.Updated = DateTime.Now;
                    globalSettings.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.GlobalSettings.Update(globalSettings);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    toastNotification.AddErrorToastMessage("Error to edit Global-settings!");
                    return View(settings);
                }
                toastNotification.AddSuccessToastMessage("Global-settings edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            toastNotification.AddErrorToastMessage("Error to edit Global-settings!");
            return View(settings);
        }
    }
}