using System.Net;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Assessor;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Assessor
{
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class CaseActionController : Controller
    {
        private readonly string baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly ApplicationDbContext _context;
        private readonly IProcessCaseService processCaseService;
        private readonly IAssessorQueryService assessorQueryService;
        private readonly IMailService mailboxService;
        private readonly INotyfService notifyService;
        private readonly ILogger<CaseActionController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public CaseActionController(ApplicationDbContext context,
            IProcessCaseService processCaseService,
            IAssessorQueryService assessorQueryService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CaseActionController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            this.processCaseService = processCaseService;
            this.assessorQueryService = assessorQueryService;
            this.mailboxService = mailboxService;
            this.notifyService = notifyService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, long claimId, string reportAiSummary = "")
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(assessorRemarks) || claimId < 1 || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (Enum.TryParse<AssessorRemarkType>(assessorRemarkType, true, out var reportUpdateStatus))
                {
                    assessorRemarks = WebUtility.HtmlEncode(assessorRemarks);
                    reportAiSummary = WebUtility.HtmlEncode(reportAiSummary);

                    var (company, contract) = await processCaseService.ProcessCaseReport(userEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                    backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseReportProcess(userEmail, claimId, baseUrl));
                    if (reportUpdateStatus == AssessorRemarkType.OK)
                    {
                        notifyService.Custom($"Case <b> #{contract}</b> Approved", 3, "green", "far fa-file-powerpoint");
                    }
                    else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                    {
                        notifyService.Custom($"Case <b>#{contract}</b> Rejected", 3, "red", "far fa-file-powerpoint");
                    }
                    else
                    {
                        notifyService.Custom($"Case <b> #{contract}</b> Re-Assigned", 3, "yellow", "far fa-file-powerpoint");
                    }
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
                else
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error withdrawing case {Id}. {UserEmail}", claimId, userEmail);
                notifyService.Error("Error processing case. Try again.");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
        }
    }
}