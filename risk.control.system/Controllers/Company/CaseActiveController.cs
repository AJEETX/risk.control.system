using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    [Breadcrumb(" Cases")]
    public class CaseActiveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly ILogger<CaseActiveController> logger;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IInvestigationService investigationService;

        public CaseActiveController(ApplicationDbContext context,
            INotyfService notifyService,
            ILogger<CaseActiveController> logger,
            IEmpanelledAgencyService empanelledAgencyService,
            IInvestigationService investigationService)
        {
            _context = context;
            this.notifyService = notifyService;
            this.logger = logger;
            this.empanelledAgencyService = empanelledAgencyService;
            this.investigationService = investigationService;
        }

        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                if (userRole.Value.Contains(AppRoles.CREATOR.ToString()))
                {
                    return RedirectToAction("Active");
                }
                else if (userRole.Value.Contains(AppRoles.ASSESSOR.ToString()))
                {
                    return RedirectToAction("Assessor");
                }
                else if (userRole.Value.Contains(AppRoles.MANAGER.ToString()))
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                var pendingCount = await _context.Investigations.CountAsync(c => c.UpdatedBy == userEmail && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS);
                return View(new JobStatus { JobId = jobId, PendingCount = pendingCount });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Active")]
        public async Task<IActionResult> ActiveDetail(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationService.GetClaimDetails(currentUserEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportTemplate(long caseId)
        {
            var template = await empanelledAgencyService.GetReportTemplate(caseId);

            return PartialView("_ReportTemplate", template);
        }
    }
}