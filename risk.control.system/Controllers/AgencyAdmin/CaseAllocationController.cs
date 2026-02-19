using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class CaseAllocationController : Controller
    {
        private readonly string baseUrl;
        private readonly IAgencyAgentAllocationService _agentAllocationService;
        private readonly INotyfService _notifyService;
        private readonly IMailService _mailService;
        private readonly ILogger<CaseAllocationController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CaseAllocationController(
            IAgencyAgentAllocationService vendorAgentAllocationService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<CaseAllocationController> logger)
        {
            _agentAllocationService = vendorAgentAllocationService;
            _notifyService = notifyService;
            _mailService = mailboxService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
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
                    _notifyService.Error("No case selected!!! Please select a case to allocate.", 3);

                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
                }

                var result = await _agentAllocationService.AllocateAsync(selectedcase, claimId, userEmail);

                if (!result.Success)
                {
                    _notifyService.Error(result.ErrorMessage ?? "OOPs !!!..Contact Admin");

                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseAssignmentToVendorAgent(userEmail, claimId, result.VendorAgentEmail, result.VendorId, baseUrl));

                _notifyService.Custom($"Case <b>#{result.ContractNumber}</b> Tasked to {result.VendorAgentEmail}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating Case {Id} by {UserEmail}", claimId, userEmail ?? "Anonymous");

                _notifyService.Error("OOPs !!!..Contact Admin");

                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}