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
    public class CaseActionController(
            IProcessCaseService processCaseService,
            IAssessorQueryService assessorQueryService,
            ICaseNotificationService caseNotificationService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CaseActionController> logger,
            IBackgroundJobClient backgroundJobClient) : Controller
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly IProcessCaseService _processCaseService = processCaseService;
        private readonly IAssessorQueryService _assessorQueryService = assessorQueryService;
        private readonly ICaseNotificationService _caseNotificationService = caseNotificationService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<CaseActionController> _logger = logger;
        private readonly IBackgroundJobClient backgroundJobClient = backgroundJobClient;

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, long claimId, string reportAiSummary = "")
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(assessorRemarks) || claimId < 1 || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assess), ControllerName<AssessorController>.Name);
            }
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!Enum.TryParse<AssessorRemarkType>(assessorRemarkType, true, out var reportUpdateStatus))
                {
                    _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AssessorController.Assess), ControllerName<AssessorController>.Name);
                }

                var (company, contract) = await _processCaseService.ProcessCaseReport(userEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                backgroundJobClient.Enqueue(() => _caseNotificationService.NotifyCaseReportProcess(userEmail, claimId, _baseUrl));
                if (reportUpdateStatus == AssessorRemarkType.OK)
                {
                    _notifyService.Custom($"Case <b> #{contract}</b> Approved", 3, "green", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AssessorController.Assess), ControllerName<AssessorController>.Name);
                }
                if (reportUpdateStatus == AssessorRemarkType.REJECT)
                {
                    _notifyService.Custom($"Case <b>#{contract}</b> Rejected", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AssessorController.Assess), ControllerName<AssessorController>.Name);
                }

                return RedirectToAction(nameof(AssessorController.Assess), ControllerName<AssessorController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing case {Id}. {UserEmail}", claimId, userEmail);
                _notifyService.Error("Error processing case. Try again.");
                return RedirectToAction(nameof(AssessorController.Assess), ControllerName<AssessorController>.Name);
            }
        }
    }
}