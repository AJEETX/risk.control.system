using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class GlobalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly IFeatureManager manager;

        public GlobalController(ApplicationDbContext context, INotyfService notifyService, IFeatureManager manager)
        {
            _context = context;
            this.notifyService = notifyService;
            this.manager = manager;
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
                notifyService.Error("Global-settings not found!");
                return NotFound();
            }
            await manager.IsEnabledAsync(nameof(FeatureFlags.SMS4ADMIN));
            if (ModelState.IsValid)
            {
                try
                {
                    var globalSettings = await _context.GlobalSettings.FirstOrDefaultAsync(x => x.GlobalSettingsId == id);
                    globalSettings.SendSMS = await manager.IsEnabledAsync(nameof(FeatureFlags.SMS4ADMIN));
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
                    notifyService.Error("Error to edit Global-settings!");
                    return View(settings);
                }
                notifyService.Success("Global-settings edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            notifyService.Error("Error to edit Global-settings!");
            return View(settings);
        }
    }
}