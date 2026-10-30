using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class CaseAllocationController(
            IAgencyAgentAllocationService agencyAgentAllocationService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            ICaseNotificationService notificationService,
            ILogger<CaseAllocationController> logger) : Controller
    {
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly IAgencyAgentAllocationService _agentAllocationService = agencyAgentAllocationService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ICaseNotificationService _notificationService = notificationService;
        private readonly ILogger<CaseAllocationController> _logger = logger;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateToAgencyAgent(string selectedcase, long caseId)
        {
            var userEmail = User?.Identity?.Name!;

            // 1. Validation Guard
            if (!ModelState.IsValid || caseId < 1)
            {
                _notifyService.Error("No case selected!!! Please select a case to allocate.", 3);
                return RedirectToAllocate();
            }

            try
            {
                // 2. Business Logic Execution
                var result = await _agentAllocationService.AllocateAsync(selectedcase, caseId, userEmail);

                // 3. Result Guard
                if (!result.Success)
                {
                    return HandleError(result.ErrorMessage);
                }

                // 4. Success Path
                ProcessSuccess(result, userEmail, caseId);
                return RedirectToAllocate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating Case {Id} by {UserEmail}", caseId, userEmail ?? "Anonymous");
                return HandleError();
            }
        }

        // Helper methods to keep the main flow clean and flat
        private void ProcessSuccess(AllocateVendorAgentResult result, string userEmail, long caseId)
        {
            _backgroundJobClient.Enqueue(() =>
                _notificationService.NotifyCaseAssignmentToVendorAgent(userEmail, caseId, result.VendorAgentEmail!, result.VendorId, _baseUrl));

            _notifyService.Custom($"Case <b>#{result.ContractNumber}</b> Tasked to {result.VendorAgentEmail}", 3, "green", "far fa-file-powerpoint");
        }

        private IActionResult HandleError(string? message = null)
        {
            _notifyService.Error(message ?? "OOPs !!!..Contact Admin");
            return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
        }

        private IActionResult RedirectToAllocate()
        {
            return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
        }
    }
}