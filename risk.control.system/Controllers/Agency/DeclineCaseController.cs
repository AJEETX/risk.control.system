using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class DeclineCaseController : Controller
    {
        private readonly string baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private IDeclineCaseService declineCaseService;
        private readonly IAgencyInvestigationDetailService vendorInvestigationDetailService;
        private readonly INotyfService notifyService;
        private readonly IMailService mailboxService;
        private readonly ILogger<DeclineCaseController> logger;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient backgroundJobClient;

        public DeclineCaseController(
            IDeclineCaseService declineCaseService,
            IAgencyInvestigationDetailService vendorInvestigationDetailService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<DeclineCaseController> logger,
            ApplicationDbContext context)
        {
            this.declineCaseService = declineCaseService;
            this.vendorInvestigationDetailService = vendorInvestigationDetailService;
            this.notifyService = notifyService;
            this.mailboxService = mailboxService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(CaseTransactionModel model, long claimId, string policyNumber)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            if (!ModelState.IsValid || model == null || claimId < 1)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            try
            {
                var agency = await declineCaseService.DeclineCaseByAgency(userEmail, model, claimId);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseWithdrawlToCompany(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case <b> #{policyNumber}</b> Declined successfully", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
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
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }

            try
            {
                var agency = await declineCaseService.WithdrawCaseFromAgent(userEmail, model, claimId);

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseWithdrawlFromAgent(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case <b> #{policyNumber}</b> Withdrawn from Agent successfully", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }
    }
}