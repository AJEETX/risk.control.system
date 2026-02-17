using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb("Cases")]
    public class CaseUploadController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILicenseService licenseService;
        private readonly ILogger<CaseUploadController> logger;
        private readonly INotyfService notifyService;

        public CaseUploadController(ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            ILicenseService licenseService,
            ILogger<CaseUploadController> logger,
            INotyfService notifyService)
        {
            this.context = context;
            this.env = env;
            _userManager = userManager;
            this.licenseService = licenseService;
            this.logger = logger;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Uploads));
        }

        [Breadcrumb(" Upload File")]
        public async Task<IActionResult> Uploads(int uploadId = 0)
        {
            var userEmail = User.Identity?.Name;
            if (!ModelState.IsValid)
            {
                return RedirectToDashboard("Invalid request.");
            }
            try
            {
                var companyUser = await context.ApplicationUser.AsNoTracking()
                    .Include(u => u.ClientCompany)
                    .Include(u => u.Country)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (companyUser == null) return RedirectToDashboard("User not found.");

                var licenseStatus = await licenseService.GetUploadPermissionsAsync(companyUser);

                if (uploadId == 0 && companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    SendLicenseNotifications(licenseStatus);
                }
                var isManager = await _userManager.IsInRoleAsync(companyUser, MANAGER.DISPLAY_NAME);
                return View(new CreateClaims
                {
                    IsManager = isManager,
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
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading log file with ID {Id}", id);
                return BadRequest(new { success = false, message = "Error downloading file: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadErrorLog(long id)
        {
            try
            {
                var file = await context.FilesOnFileSystem.FirstOrDefaultAsync(x => x.Id == id);
                if (file == null || file.ErrorByteData == null)
                    return NotFound("File not found");

                var fileName = $"{file.Name}_UploadError_{id}.csv";
                Response.Headers.Append("X-File-Name", fileName);
                Response.Headers.Append("Access-Control-Expose-Headers", "X-File-Name");
                return File(file.ErrorByteData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading error log file with ID {Id}", id);
                return BadRequest(new { success = false, message = "Error downloading file: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLog(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Error deleting file" });
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var companyUser = await context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                var file = await context.FilesOnFileSystem.AsNoTracking().Include(c => c.CaseIds).FirstOrDefaultAsync(f => f.Id == id);
                if (file == null)
                {
                    return NotFound(new { success = false, message = "File not found." });
                }

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