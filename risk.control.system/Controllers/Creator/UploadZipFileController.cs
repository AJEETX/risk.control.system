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
        private readonly string baseUrl;

        private readonly IUploadZipFileService uploadZipFileService;
        private readonly IZipFileService zipFileService;

        private readonly INotyfService notifyService;
        private readonly ILogger<UploadZipFileController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public UploadZipFileController(
            IUploadZipFileService uploadZipFileService,
            IZipFileService zipFileService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UploadZipFileController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            //this.ftpService = ftpService;
            this.zipFileService = zipFileService;
            this.notifyService = notifyService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(IFormFile postedFile, CreateClaims model)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip")
                {
                    notifyService.Custom($"Invalid File Upload Error. ", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseUploadController.Uploads), ControllerName<CaseUploadController>.Name);
                }

                var uploadId = await zipFileService.Save(currentUserEmail, postedFile, CREATEDBY.AUTO, model.UploadAndAssign);
                var jobId = backgroundJobClient.Enqueue<IUploadZipFileService>(service => service.StartFileUpload(currentUserEmail, uploadId, baseUrl, model.UploadAndAssign));

                if (!model.UploadAndAssign)
                {
                    notifyService.Custom($"Uploading ...", 2, "#17A2B8", "fa fa-upload");
                }
                else
                {
                    notifyService.Custom($"Assigning ...", 2, "#dc3545", "fa fa-upload");
                }

                return RedirectToAction(nameof(CaseUploadController.Uploads), ControllerName<CaseUploadController>.Name, new { uploadId = uploadId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "File Upload Error for {User}", currentUserEmail);
                notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
                return RedirectToAction(nameof(CaseUploadController.Uploads), ControllerName<CaseUploadController>.Name);
            }
        }
    }
}