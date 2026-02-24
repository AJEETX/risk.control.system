using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    [Breadcrumb("Cases")]
    public class CaseActiveController : Controller
    {
        private readonly ICaseActiveService _caseActiveService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<CaseActiveController> _logger;
        private readonly IReportTemplateService _reportTemplateService;

        public CaseActiveController(
            ICaseActiveService caseActiveService,
            INotyfService notifyService,
            ILogger<CaseActiveController> logger,
            IReportTemplateService reportTemplateService)
        {
            _caseActiveService = caseActiveService;
            _notifyService = notifyService;
            _logger = logger;
            _reportTemplateService = reportTemplateService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Active));
        }

        [HttpGet]
        public IActionResult GetJobStatus(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return Json(new { status = "Invalid Job ID" });
            }

            using (var connection = JobStorage.Current.GetConnection())
            {
                var state = connection.GetStateData(jobId);

                string jobStatus = state?.Name ?? "Not Found";

                return Json(new { jobId, status = jobStatus });
            }
        }

        [Breadcrumb(title: "Active")]
        public async Task<IActionResult> Active(string jobId = "")
        {
            var userEmail = HttpContext.User.Identity.Name;
            try
            {
                var pendingCount = await _caseActiveService.GetPendingUploadCount(userEmail);
                return View(new JobStatus { JobId = jobId, PendingCount = pendingCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting active case {JobId}. {UserEmail}", jobId, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Details", FromAction = nameof(Active))]
        public async Task<IActionResult> ActiveDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id <= 0)
            {
                _notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await _caseActiveService.GetActiveCaseDetails(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred active case detail {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportTemplate(long caseId)
        {
            if (caseId <= 0)
            {
                return BadRequest("Invalid case id.");
            }

            try
            {
                var template = await _reportTemplateService.GetReportTemplate(caseId);

                if (template == null)
                {
                    return NotFound();
                }

                return PartialView("_ReportTemplate", template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report template. CaseId: {CaseId}, User: {UserEmail}", caseId, User.Identity?.Name ?? "Anonymous");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}