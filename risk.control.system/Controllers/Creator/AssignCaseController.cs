using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Services;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    public class AssignCaseController : Controller
    {
        private readonly string _baseUrl;
        private readonly IAssignCaseService _assignCaseService;
        private readonly IMailService _mailService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<AssignCaseController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AssignCaseController(
            IAssignCaseService assignCaseService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AssignCaseController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _assignCaseService = assignCaseService;
            _mailService = mailboxService;
            _notifyService = notifyService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuto(List<long> claims)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || claims == null || claims.Count == 0)
                {
                    _notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                // AUTO ALLOCATION COUNT
                var distinctClaims = claims.Distinct().ToList();
                var affectedRows = await _assignCaseService.UpdateCaseAllocationStatus(userEmail, distinctClaims);
                if (affectedRows < distinctClaims.Count)
                {
                    _notifyService.Custom($"Case(s) assignment error", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }
                var jobId = _backgroundJobClient.Enqueue(() => _assignCaseService.BackgroundAutoAllocation(distinctClaims, userEmail, _baseUrl));
                _notifyService.Custom($"Assignment of <b> {distinctClaims.Count}</b> Case(s) started", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CaseActiveController.Active), ControllerName<CaseActiveController>.Name, new { jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting case(s). {UserEmail}", userEmail);
                _notifyService.Error("Error assigning case. Try again.");
            }
            return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AllocateSingle2Vendor(long selectedcase, long caseId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1 || caseId < 1)
                {
                    _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                var (policy, status, agencyName) = await _assignCaseService.AllocateToVendor(userEmail, caseId, selectedcase, false);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                var jobId = _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseAllocationToVendorAndManager(userEmail, policy, caseId, selectedcase, _baseUrl));

                _notifyService.Custom($"Case <b>#{policy}</b> <i>{status}</i> to {agencyName}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseActiveController.Active), ControllerName<CaseActiveController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning case {Id} to {Agency}. {UserEmail}", caseId, selectedcase, userEmail);
                _notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAutoSingle(long claims)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || claims < 1)
                {
                    _notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                var allocatedCaseNumber = await _assignCaseService.ProcessAutoSingleAllocation(claims, userEmail, _baseUrl);
                if (string.IsNullOrWhiteSpace(allocatedCaseNumber))
                {
                    _notifyService.Custom($"Case #:{allocatedCaseNumber} Not Assigned", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }
                _notifyService.Custom($"Case <b>#:{allocatedCaseNumber}</b> Assigned<sub>auto</b>", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning case {Id}. {UserEmail}", claims, userEmail);
                _notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
            return RedirectToAction(nameof(CaseActiveController.Active), ControllerName<CaseActiveController>.Name);
        }
    }
}