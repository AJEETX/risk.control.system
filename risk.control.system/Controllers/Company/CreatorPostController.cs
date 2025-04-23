//using AspNetCoreHero.ToastNotification.Notyf;
//using System.Security.Claims;
//using Microsoft.AspNetCore.Mvc;

//using risk.control.system.AppConstant;

//using SmartBreadcrumbs.Attributes;
//using AspNetCoreHero.ToastNotification.Abstractions;
//using risk.control.system.Data;
//using risk.control.system.Services;
//using static risk.control.system.AppConstant.Applicationsettings;
//using Microsoft.EntityFrameworkCore;
//using Google.Api;
//using risk.control.system.Models.ViewModel;
//using risk.control.system.Models;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using SmartBreadcrumbs.Nodes;
//using risk.control.system.Helpers;
//using Microsoft.AspNetCore.Authorization;
//using Hangfire;
//using Amazon.Textract;
//using Microsoft.AspNetCore.Http;

//namespace risk.control.system.Controllers.Company
//{
//    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
//    public class CreatorPostController : Controller
//    {
//        private const string CLAIMS = "claims";
//        private readonly ApplicationDbContext _context;
//        private readonly IEmpanelledAgencyService empanelledAgencyService;
//        private readonly ICustomApiCLient customApiCLient;
//        private readonly IClaimCreationService creationService;
//        private readonly ICreatorService creatorService;
//        private readonly IFtpService ftpService;
//        private readonly INotyfService notifyService;
//        private readonly IHttpContextAccessor httpContextAccessor;
//        private readonly IProgressService progressService;
//        private readonly IBackgroundJobClient backgroundJobClient;
//        private readonly IClaimPolicyService claimPolicyService;

//        public CreatorPostController(ApplicationDbContext context,
//            IEmpanelledAgencyService empanelledAgencyService,
//            ICustomApiCLient customApiCLient,
//            IClaimCreationService creationService,
//            ICreatorService creatorService,
//            IFtpService ftpService,
//            INotyfService notifyService,
//            IHttpContextAccessor httpContextAccessor,
//            IInvestigationReportService investigationReportService,
//            IProgressService progressService,
//            IBackgroundJobClient backgroundJobClient,
//            IClaimPolicyService claimPolicyService)
//        {
//            _context = context;
//            this.claimPolicyService = claimPolicyService;
//            this.empanelledAgencyService = empanelledAgencyService;
//            this.customApiCLient = customApiCLient;
//            this.creationService = creationService;
//            this.creatorService = creatorService;
//            this.ftpService = ftpService;
//            this.notifyService = notifyService;
//            this.httpContextAccessor = httpContextAccessor;
//            this.investigationReportService = investigationReportService;
//            this.progressService = progressService;
//            this.backgroundJobClient = backgroundJobClient;
//        }
//        [HttpPost]
//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> New(IFormFile postedFile, CreateClaims model)
//        {
//            try
//            {
//                if (postedFile == null || model == null ||
//                string.IsNullOrWhiteSpace(Path.GetFileName(postedFile.FileName)) ||
//                string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(postedFile.FileName))) ||
//                Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip"
//                )
//                {
//                    notifyService.Custom($"Invalid File Upload Error. ", 3, "red", "far fa-file-powerpoint");
//                    return RedirectToAction(nameof(ClaimsLogController.Uploads), "ClaimsLog");
//                }

//                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
//                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
//                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
//                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

//                var uploadId = await ftpService.UploadFile(currentUserEmail, postedFile, model.CREATEDBY, model.UploadAndAssign);
//                var jobId = backgroundJobClient.Enqueue(() => ftpService.StartUpload(currentUserEmail, uploadId, baseUrl,model.UploadAndAssign));
//                progressService.AddUploadJob(jobId, currentUserEmail);
//                if(!model.UploadAndAssign)
//                {
//                    notifyService.Custom($"Upload in progress ", 3, "#17A2B8", "fa fa-upload");
//                }
//                else
//                {
//                    notifyService.Custom($"Direct Assign in progress ", 5, "#dc3545", "fa fa-upload");

//                }

//                return RedirectToAction(nameof(ClaimsLogController.Uploads), "ClaimsLog", new { uploadId = uploadId });

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
//                return RedirectToAction(nameof(ClaimsLogController.Uploads), "ClaimsLog");
//            }
//        }

//        [HttpGet]
//        public IActionResult GetJobStatus()
//        {
//            var currentUserEmail = HttpContext.User?.Identity?.Name;
//            var jobIds = progressService.GetUploadJobIds(currentUserEmail);

//            if (jobIds == null || jobIds.Count == 0)
//            {
//                return Json(new { jobId = "", status = "Not Found" });
//            }

//            using (var connection = JobStorage.Current.GetConnection())
//            {
//                foreach (var jobId in jobIds)
//                {
//                    var state = connection.GetStateData(jobId);
//                    string jobStatus = state?.Name ?? "Not Found";

//                    // Return first active job (Processing or Enqueued)
//                    if (jobStatus == "Processing" || jobStatus == "Enqueued")
//                    {
//                        return Json(new { jobId, status = jobStatus });
//                    }
//                }
//            }

//            // If no active jobs are found, return the last completed job
//            return Json(new { jobId = jobIds.Last(), status = "Completed or Failed" });
//        }

//        public IActionResult GetJobProgress(int jobId)
//        {
//            int progress = progressService.GetProgress(jobId);
//            return Json(new { progress });
//        }

//        public IActionResult GetAssignmentProgress(string jobId)
//        {
//            int progress = progressService.GetAssignmentProgress(jobId);
//            return Json(new { progress });
//        }


//        [HttpPost, ActionName("DeleteCases")]
//        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
//        public async Task<IActionResult> DeleteCases([FromBody] DeleteRequestModel request)
//        {
//            if (request.claims == null || request.claims.Count == 0)
//            {
//                return Json(new { success = false, message = "No cases selected for deletion." });
//            }

//            try
//            {
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                foreach (var claim in request.claims)
//                {
//                    var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(claim);
//                    if (claimsInvestigation == null)
//                    {
//                        notifyService.Error("Not Found!!!..Contact Admin");
//                        return RedirectToAction(nameof(Index), "Dashboard");
//                    }

//                    claimsInvestigation.Updated = DateTime.Now;
//                    claimsInvestigation.UpdatedBy = currentUserEmail;
//                    claimsInvestigation.Deleted = true;
//                    _context.ClaimsInvestigation.Update(claimsInvestigation);
//                }
//                await _context.SaveChangesAsync();

//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        public class DeleteRequestModel
//        {
//            public List<string> claims { get; set; }
//        }

//    }
//}
