using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    public class UploadZipFileController : Controller
    {
        private readonly string _baseUrl;

        private readonly IZipFileService _zipFileService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<UploadZipFileController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public UploadZipFileController(
            IZipFileService zipFileService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UploadZipFileController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _zipFileService = zipFileService;
            _notifyService = notifyService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(IFormFile postedFile, CreateClaims model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip")
                {
                    _notifyService.Custom($"Invalid File Upload Error. ", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseUploadController.Uploads), ControllerName<CaseUploadController>.Name);
                }

                var uploadId = await _zipFileService.Save(userEmail, postedFile, CREATEDBY.AUTO, model.UploadAndAssign);
                var jobId = _backgroundJobClient.Enqueue<IUploadZipFileService>(service => service.StartFileUpload(userEmail, uploadId, _baseUrl, model.UploadAndAssign));

                if (!model.UploadAndAssign)
                {
                    _notifyService.Custom($"Uploading Case(s)...", 2, "#17A2B8", "fa fa-upload");
                }
                else
                {
                    _notifyService.Custom($"Assigning Case(s)...", 2, "#dc3545", "fa fa-upload");
                }

                return RedirectToAction(nameof(CaseUploadController.Uploads), ControllerName<CaseUploadController>.Name, new { uploadId = uploadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File Upload Error for {User}", userEmail);
                _notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
                return RedirectToAction(nameof(CaseUploadController.Uploads), ControllerName<CaseUploadController>.Name);
            }
        }
    }
}