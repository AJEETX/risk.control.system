using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb("Cases")]
    public class CaseUploadController : Controller
    {
        private readonly ICaseUploadService _caseUploadService;
        private readonly ILogger<CaseUploadController> _logger;
        private readonly INotyfService _notifyService;

        public CaseUploadController(
            ICaseUploadService caseUploadService,
            ILogger<CaseUploadController> logger,
            INotyfService notifyService)
        {
            _caseUploadService = caseUploadService;
            _logger = logger;
            _notifyService = notifyService;
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
                var result = await _caseUploadService.GetUploadViewDataAsync(userEmail, uploadId);

                if (result == null) return RedirectToDashboard("File Upload failed.");

                if (result.ShouldSendTrialNotification)
                {
                    SendLicenseNotifications(result.LicenseStatus);
                }

                return View(new CreateClaims
                {
                    IsManager = result.IsManager,
                    UserCanCreate = result.UserCanCreate,
                    HasClaims = result.HasClaims,
                    FileSampleIdentifier = result.FileSampleIdentifier,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Uploads page for {User}", userEmail);
                return RedirectToDashboard("An unexpected error occurred.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadLog(long id)
        {
            try
            {
                var result = await _caseUploadService.GetDownloadLogAsync(id);

                if (!result.Success)
                {
                    return NotFound(result.ErrorMessage);
                }

                // Add custom headers for the client-side filename extraction
                Response.Headers.Append("X-File-Name", result.FileName);
                Response.Headers.Append("Access-Control-Expose-Headers", "X-File-Name");

                // The 'File' method automatically handles the disposal of the stream
                // once the download is complete.
                return File(result.FileStream, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading log file with ID {Id}", id);
                return BadRequest(new { success = false, message = "Error downloading file: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadErrorLog(long id)
        {
            try
            {
                var result = await _caseUploadService.GetDownloadErrorLogAsync(id);

                if (!result.Success)
                {
                    return NotFound(result.ErrorMessage);
                }

                // Standardize headers for frontend consumption
                Response.Headers.Append("X-File-Name", result.FileName);
                Response.Headers.Append("Access-Control-Expose-Headers", "X-File-Name");

                return File(result.FileBytes, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading error log file with ID {Id}", id);
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
                var (success, message) = await _caseUploadService.DeleteLogAsync(id, userEmail);

                if (!success)
                {
                    return NotFound(new { success, message });
                }

                return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return BadRequest(new { success = false, message = "Error deleting file: " + ex.Message });
            }
        }

        private void SendLicenseNotifications(LicenseStatus status)
        {
            if (!status.CanCreate)
                _notifyService.Warning($"MAX Case limit = <b>{status.MaxAllowed}</b> reached");
            else
                _notifyService.Information($"Limit available = <b>{status.AvailableCount}</b>");
        }

        private IActionResult RedirectToDashboard(string errorMessage, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(logDetail))
            {
                _logger.LogWarning(logDetail);
            }

            _notifyService.Error(errorMessage);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}