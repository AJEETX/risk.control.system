using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class CaseAllocationController : Controller
    {
        private readonly string baseUrl;
        private readonly IAgencyAgentAllocationService vendorAgentAllocationService;
        private readonly INotyfService notifyService;
        private readonly IMailService mailboxService;
        private readonly ILogger<CaseAllocationController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public CaseAllocationController(
            IAgencyAgentAllocationService vendorAgentAllocationService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<CaseAllocationController> logger)
        {
            this.vendorAgentAllocationService = vendorAgentAllocationService;
            this.notifyService = notifyService;
            this.mailboxService = mailboxService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, long claimId)
        {
            var userEmail = User?.Identity?.Name;

            try
            {
                if (!ModelState.IsValid || claimId < 1)
                {
                    notifyService.Error("No case selected!!! Please select a case to allocate.", 3);

                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }

                var result = await vendorAgentAllocationService.AllocateAsync(selectedcase, claimId, userEmail);

                if (!result.Success)
                {
                    notifyService.Error(result.ErrorMessage ?? "OOPs !!!..Contact Admin");

                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToVendorAgent(userEmail, claimId, result.VendorAgentEmail, result.VendorId, baseUrl));

                notifyService.Custom($"Case <b>#{result.ContractNumber}</b> Tasked to {result.VendorAgentEmail}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error allocating Case {Id} by {UserEmail}", claimId, userEmail ?? "Anonymous");

                notifyService.Error("OOPs !!!..Contact Admin");

                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}