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
    public class DeclineCaseController : Controller
    {
        private readonly string _baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private IDeclineCaseService _declineCaseService;
        private readonly IAgencyInvestigationDetailService _agencyInvestigationDetailService;
        private readonly INotyfService _notifyService;
        private readonly IMailService _mailService;
        private readonly ILogger<DeclineCaseController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public DeclineCaseController(
            IDeclineCaseService declineCaseService,
            IAgencyInvestigationDetailService agencyInvestigationDetailService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailService,
            ILogger<DeclineCaseController> logger)
        {
            _declineCaseService = declineCaseService;
            _agencyInvestigationDetailService = agencyInvestigationDetailService;
            _notifyService = notifyService;
            _mailService = mailService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(CaseTransactionModel model, long claimId, string policyNumber)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            if (!ModelState.IsValid || model == null || claimId < 1)
            {
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            try
            {
                var agency = await _declineCaseService.DeclineCaseByAgency(userEmail, model, claimId);

                _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseWithdrawlToCompany(userEmail, claimId, agency.VendorId, _baseUrl));

                _notifyService.Custom($"Case <b> #{policyNumber}</b> Declined successfully", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCaseFromAgent(CaseTransactionModel model, long claimId, string policyNumber)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            if (!ModelState.IsValid || claimId < 1)
            {
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }

            try
            {
                var agency = await _declineCaseService.WithdrawCaseFromAgent(userEmail, model, claimId);

                var jobId = _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseWithdrawlFromAgent(userEmail, claimId, agency.VendorId, _baseUrl));

                _notifyService.Custom($"Case <b> #{policyNumber}</b> Withdrawn from Agent successfully", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }
    }
}