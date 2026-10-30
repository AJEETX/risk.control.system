using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class DeclineCaseController(
            IDeclineCaseService declineCaseService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            ICaseNotificationService notificationService,
            ILogger<DeclineCaseController> logger) : Controller
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private IDeclineCaseService _declineCaseService = declineCaseService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ICaseNotificationService _notificationService = notificationService;
        private readonly ILogger<DeclineCaseController> _logger = logger;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(CaseTransactionModel model, long caseId, string policyNumber)
        {
            string userEmail = HttpContext?.User?.Identity?.Name!;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            try
            {
                var agency = await _declineCaseService.DeclineCaseByAgency(userEmail, model, caseId);

                _backgroundJobClient.Enqueue(() => _notificationService.NotifyCaseDeclineByAgency(userEmail, policyNumber, caseId, agency.VendorId, _baseUrl));

                _notifyService.Custom($"Case <b> #{policyNumber}</b> Declined successfully", 3, "white", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", caseId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCaseFromAgent(CaseTransactionModel model, long caseId, string policyNumber)
        {
            string userEmail = HttpContext?.User?.Identity?.Name!;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }

            try
            {
                var agency = await _declineCaseService.WithdrawCaseFromAgent(userEmail, model, caseId);

                var jobId = _backgroundJobClient.Enqueue(() => _notificationService.NotifyCaseWithdrawlFromAgent(userEmail, caseId, agency.VendorId, _baseUrl));

                _notifyService.Custom($"Case <b> #{policyNumber}</b> Withdrawn from Agent successfully", 3, "white", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", caseId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }
    }
}