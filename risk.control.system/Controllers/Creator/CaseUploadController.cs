using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb(" Cases")]
    public class CaseUploadController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly ILicenseService licenseService;
        private readonly ILogger<CaseUploadController> logger;
        private readonly INotyfService notifyService;

        public CaseUploadController(ApplicationDbContext context,
            IWebHostEnvironment env,
            ILicenseService licenseService,
            ILogger<CaseUploadController> logger,
            INotyfService notifyService)
        {
            this.context = context;
            this.env = env;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadLog(long id)
        {
            var file = await context.FilesOnFileSystem.FirstOrDefaultAsync(x => x.Id == id);
            if (file == null || string.IsNullOrWhiteSpace(file.FilePath))
                return NotFound("File not found");

            var fullPath = Path.Combine(env.ContentRootPath, file.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound("File missing on server");

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            Response.Headers.Append("X-File-Name", file.Name);
            Response.Headers.Append("Access-Control-Expose-Headers", "X-File-Name");

            return File(stream, "application/zip", file.Name);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadErrorLog(long id)
        {
            var file = await context.FilesOnFileSystem.FirstOrDefaultAsync(x => x.Id == id);
            if (file == null || file.ErrorByteData == null)
                return NotFound();

            var fileName = $"{file.Name}_UploadError_{id}.csv";
            Response.Headers.Append("X-File-Name", fileName);
            Response.Headers.Append("Access-Control-Expose-Headers", "X-File-Name");
            return File(file.ErrorByteData, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLog(int id)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPs !!!.. Download error");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
            var file = await context.FilesOnFileSystem.Include(c => c.CaseIds).FirstOrDefaultAsync(f => f.Id == id);
            if (file == null)
            {
                return NotFound(new { success = false, message = "File not found." });
            }

            try
            {
                if (System.IO.File.Exists(file.FilePath))
                {
                    System.IO.File.Delete(file.FilePath); // Delete the file from storage
                }
                file.Deleted = true;
                context.FilesOnFileSystem.Update(file);
                await context.SaveChangesAsync();

                return Ok(new { success = true, message = "File deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting file");
                return BadRequest(new { success = false, message = "Error deleting file: " + ex.Message });
            }
        }

        private IActionResult HandleUnauthorizedAccess(string logMessage)
        {
            logger.LogWarning(logMessage);
            notifyService.Error("OOPs !!!..Contact Admin");
            return RedirectToAction("Index", "Dashboard");
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