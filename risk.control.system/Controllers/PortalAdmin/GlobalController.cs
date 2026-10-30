using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Breadcrumb("General Setup")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class GlobalController(ApplicationDbContext context, INotyfService notifyService, IFeatureManager featureManager, ILogger<GlobalController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly INotyfService _notifyService = notifyService;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly ILogger<GlobalController> _logger = logger;

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
                _notifyService.Error("Global-settings not found!");
                return NotFound();
            }
            await _featureManager.IsEnabledAsync(nameof(FeatureFlags.SMS4ADMIN));
            if (ModelState.IsValid)
            {
                try
                {
                    var globalSettings = await _context.GlobalSettings.FirstOrDefaultAsync(x => x.GlobalSettingsId == id);
                    globalSettings!.SendSMS = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.SMS4ADMIN));
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
                    globalSettings.Updated = DateTime.UtcNow;
                    globalSettings.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.GlobalSettings.Update(globalSettings);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred.");
                    _notifyService.Error("Error to edit Global-settings!");
                    return View(settings);
                }
                _notifyService.Success("Global-settings edited successfully!");
                return RedirectToAction(nameof(Index));
            }
            _notifyService.Error("Error to edit Global-settings!");
            return View(settings);
        }
    }
}