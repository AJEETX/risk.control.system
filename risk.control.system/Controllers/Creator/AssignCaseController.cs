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
        public async Task<IActionResult> AssignAuto(List<long> cases)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || cases == null || cases.Count == 0)
                {
                    _notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }

                // AUTO ALLOCATION COUNT
                var distinctCases = cases.Distinct().ToList();
                var affectedRows = await _assignCaseService.UpdateCaseAllocationStatus(userEmail, distinctCases);
                if (affectedRows < distinctCases.Count)
                {
                    _notifyService.Custom($"Case(s) assignment error", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }
                var jobId = _backgroundJobClient.Enqueue(() => _assignCaseService.BackgroundAutoAllocation(distinctCases, userEmail, _baseUrl));
                _notifyService.Custom($"Assignment of <b> {distinctCases.Count}</b> Case(s) started", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CaseActiveController.Active), ControllerName<CaseActiveController>.Name, new { jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting case(s). {UserEmail}", userEmail);
                _notifyService.Error("Error assigning case. Try again.");
            }
            return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AllocateSingle2Vendor(long selectedcase, long caseId)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1 || caseId < 1)
                {
                    _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }

                var (policy, status, agencyName) = await _assignCaseService.AllocateToVendor(userEmail, caseId, selectedcase, false);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    _notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }

                var jobId = _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseAllocationToVendorAndManager(userEmail, policy, caseId, selectedcase, _baseUrl));

                _notifyService.Custom($"Case <b>#{policy}</b> <i>{status}</i> to {agencyName}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseActiveController.Active), ControllerName<CaseActiveController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning case {Id} to {Agency}. {UserEmail}", caseId, selectedcase, userEmail);
                _notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAutoSingle(long caseId)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || caseId < 1)
                {
                    _notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }

                var allocatedCaseNumber = await _assignCaseService.ProcessAutoSingleAllocation(caseId, userEmail, _baseUrl);
                if (string.IsNullOrWhiteSpace(allocatedCaseNumber))
                {
                    _notifyService.Custom($"Case #:{allocatedCaseNumber} Not Assigned", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }
                _notifyService.Custom($"Case <b>#:{allocatedCaseNumber}</b> Assigned<sub>auto</b>", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning case {Id}. {UserEmail}", caseId, userEmail);
                _notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
            return RedirectToAction(nameof(CaseActiveController.Active), ControllerName<CaseActiveController>.Name);
        }
    }
}