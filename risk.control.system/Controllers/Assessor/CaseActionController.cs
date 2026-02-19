using System.Net;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Assessor;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Assessor
{
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class CaseActionController : Controller
    {
        private readonly string _baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly IProcessCaseService _processCaseService;
        private readonly IAssessorQueryService _assessorQueryService;
        private readonly IMailService _mailService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<CaseActionController> _logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public CaseActionController(
            IProcessCaseService processCaseService,
            IAssessorQueryService assessorQueryService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CaseActionController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _processCaseService = processCaseService;
            _assessorQueryService = assessorQueryService;
            _mailService = mailboxService;
            _notifyService = notifyService;
            _logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, long claimId, string reportAiSummary = "")
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(assessorRemarks) || claimId < 1 || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), ControllerName<AssessorController>.Name);
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (Enum.TryParse<AssessorRemarkType>(assessorRemarkType, true, out var reportUpdateStatus))
                {
                    assessorRemarks = WebUtility.HtmlEncode(assessorRemarks);
                    reportAiSummary = WebUtility.HtmlEncode(reportAiSummary);

                    var (company, contract) = await _processCaseService.ProcessCaseReport(userEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                    backgroundJobClient.Enqueue(() => _mailService.NotifyCaseReportProcess(userEmail, claimId, _baseUrl));
                    if (reportUpdateStatus == AssessorRemarkType.OK)
                    {
                        _notifyService.Custom($"Case <b> #{contract}</b> Approved", 3, "green", "far fa-file-powerpoint");
                    }
                    else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                    {
                        _notifyService.Custom($"Case <b>#{contract}</b> Rejected", 3, "red", "far fa-file-powerpoint");
                    }
                    else
                    {
                        _notifyService.Custom($"Case <b> #{contract}</b> Re-Assigned", 3, "yellow", "far fa-file-powerpoint");
                    }
                    return RedirectToAction(nameof(AssessorController.Assessor), ControllerName<AssessorController>.Name);
                }
                else
                {
                    _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AssessorController.Assessor), ControllerName<AssessorController>.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing case {Id}. {UserEmail}", claimId, userEmail);
                _notifyService.Error("Error processing case. Try again.");
                return RedirectToAction(nameof(AssessorController.Assessor), ControllerName<AssessorController>.Name);
            }
        }
    }
}