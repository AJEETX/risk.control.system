using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}, {MANAGER.DISPLAY_NAME}")]
    [Breadcrumb(" Cases")]
    public class CaseActiveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<CaseActiveController> logger;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IInvestigationDetailService investigationDetailService;

        public CaseActiveController(ApplicationDbContext context,
            INotyfService notifyService,
            ILogger<CaseActiveController> logger,
            IEmpanelledAgencyService empanelledAgencyService,
            IInvestigationDetailService investigationDetailService)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
            this.empanelledAgencyService = empanelledAgencyService;
            this.investigationDetailService = investigationDetailService;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("UnAuthenticated User");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                if (userRole.Value.Contains(CREATOR.DISPLAY_NAME))
                {
                    return RedirectToAction("Active");
                }
                else if (userRole.Value.Contains(ASSESSOR.DISPLAY_NAME))
                {
                    return RedirectToAction("Assessor");
                }
                else if (userRole.Value.Contains(MANAGER.DISPLAY_NAME))
                {
                    return RedirectToAction("Manager");
                }
                else
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting active case(s). {UserEmail}", userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
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
                var pendingCount = await _context.Investigations.CountAsync(c => c.UpdatedBy == userEmail && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS);
                return View(new JobStatus { JobId = jobId, PendingCount = pendingCount });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting active case {JobId}. {UserEmail}", jobId, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Active")]
        public async Task<IActionResult> ActiveDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var currentUser = await _context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await investigationDetailService.GetCaseDetails(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred active case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
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
                var template = await empanelledAgencyService.GetReportTemplate(caseId);

                if (template == null)
                {
                    return NotFound();
                }

                return PartialView("_ReportTemplate", template);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error getting report template. CaseId: {CaseId}, User: {UserEmail}",
                    caseId,
                    User.Identity?.Name ?? "Anonymous");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}