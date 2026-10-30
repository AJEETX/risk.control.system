using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    [Route("[controller]")]
    public class WithdrawCaseController(
            IWithdrawCaseService withdrawCaseService,
            ICaseNotificationService caseNotificationService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WithdrawCaseController> logger,
            IBackgroundJobClient backgroundJobClient) : Controller
    {
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly IWithdrawCaseService _withdrawCaseService = withdrawCaseService;
        private readonly ICaseNotificationService _caseNotificationService = caseNotificationService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<WithdrawCaseController> _logger = logger;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(CaseTransactionModel model, long caseId)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || model == null || caseId < 1)
                {
                    _notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
                }

                var (policyNumber, vendorId) = await _withdrawCaseService.WithdrawCaseByCompany(userEmail, model, caseId);

                _backgroundJobClient.Enqueue(() => _caseNotificationService.NotifyCaseWithdrawlByCompany(userEmail, policyNumber, caseId, vendorId, _baseUrl));

                _notifyService.Custom($"Case <b> #{policyNumber}</b>  withdrawn successfully", 3, "white", "far fa-file-powerpoint");

                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing case {Id}. {UserEmail}", caseId, userEmail);
                _notifyService.Error("Error withdrawing case. Try again.");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
        }
        //[ValidateAntiForgeryToken]
        [HttpPost("WithdrawClaim/{id}")]
        public async Task<IActionResult> WithdrawClaim(int id, [FromBody] WithdrawClaimRequest request)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            if (string.IsNullOrWhiteSpace(request?.Reason))
            {
                return BadRequest(new { message = "Withdrawal reason is required." });
            }
            var (policyNumber, vendorId) = await _withdrawCaseService.WithdrawCaseByCompanyNew(userEmail, request);

            _backgroundJobClient.Enqueue(() => _caseNotificationService.NotifyCaseWithdrawlByCompanyNew(userEmail, policyNumber, request.ClaimId, vendorId, _baseUrl));

            return Ok(new { success = true, policyNumber = policyNumber });
        }
    }
}