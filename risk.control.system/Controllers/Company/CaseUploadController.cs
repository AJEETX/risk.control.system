using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb(" Cases")]
    public class CaseUploadController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly ILicenseService licenseService;
        private readonly ILogger<CaseUploadController> logger;
        private readonly INotyfService notifyService;

        public CaseUploadController(ApplicationDbContext context,
            ILicenseService licenseService,
            ILogger<CaseUploadController> logger,
            INotyfService notifyService)
        {
            this.context = context;
            this.licenseService = licenseService;
            this.logger = logger;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return HandleUnauthorizedAccess("User identity not found.");
                }

                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                return role switch
                {
                    var r when r.Contains(CREATOR.DISPLAY_NAME) => RedirectToAction("Uploads"),
                    var r when r.Contains(MANAGER.DISPLAY_NAME) => RedirectToAction("Manager"),
                    _ => RedirectToAction("Index", "Dashboard")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Routing error for user: {User}", User?.Identity?.Name);
                notifyService.Error("An unexpected error occurred. Please contact the administrator.");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        private IActionResult HandleUnauthorizedAccess(string logMessage)
        {
            logger.LogWarning(logMessage);
            notifyService.Error("OOPs !!!..Contact Admin");
            return RedirectToAction("Index", "Dashboard");
        }

        [Breadcrumb(" Upload File")]
        public async Task<IActionResult> Uploads(int uploadId = 0)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return HandleUnauthorizedAccess("User identity not found.");
            }
            if (!ModelState.IsValid)
            {
                return RedirectToDashboard("Invalid request.");
            }
            try
            {
                var companyUser = await context.ApplicationUser
                    .Include(u => u.ClientCompany)
                    .Include(u => u.Country)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (companyUser == null) return RedirectToDashboard("User not found.");

                // Move business logic to a specialized service
                var licenseStatus = await licenseService.GetUploadPermissionsAsync(companyUser);

                // Handle Notifications only if this isn't a post-upload redirect (uploadId == 0)
                if (uploadId == 0 && companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    SendLicenseNotifications(licenseStatus);
                }

                return View(new CreateClaims
                {
                    BulkUpload = companyUser.ClientCompany.BulkUpload,
                    UserCanCreate = licenseStatus.CanCreate,
                    HasClaims = licenseStatus.HasClaimsPending,
                    FileSampleIdentifier = companyUser.Country?.Code?.ToLower() ?? "default",
                    AutoAllocation = companyUser.ClientCompany.AutoAllocation
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading Uploads page for {User}", userEmail);
                return RedirectToDashboard("An unexpected error occurred.");
            }
        }

        private void SendLicenseNotifications(LicenseStatus status)
        {
            if (!status.CanCreate)
                notifyService.Warning($"MAX Case limit = <b>{status.MaxAllowed}</b> reached");
            else
                notifyService.Information($"Limit available = <b>{status.AvailableCount}</b>");
        }

        private IActionResult RedirectToDashboard(string errorMessage, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(logDetail))
            {
                logger.LogWarning(logDetail);
            }

            notifyService.Error(errorMessage);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}