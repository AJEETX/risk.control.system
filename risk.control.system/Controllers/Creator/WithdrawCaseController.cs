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
    public class WithdrawCaseController : Controller
    {
        private readonly string _baseUrl;
        private readonly IWithdrawCaseService _withdrawCaseService;
        private readonly IMailService _mailboxService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<WithdrawCaseController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public WithdrawCaseController(
            IWithdrawCaseService withdrawCaseService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WithdrawCaseController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _withdrawCaseService = withdrawCaseService;
            _mailboxService = mailboxService;
            _notifyService = notifyService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(CaseTransactionModel model, long caseId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || model == null || caseId < 1)
                {
                    _notifyService.Error("OOPs !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var (policyNumber, vendorId) = await _withdrawCaseService.WithdrawCaseByCompany(userEmail, model, caseId);

                _backgroundJobClient.Enqueue(() => _mailboxService.NotifyCaseWithdrawlToCompany(userEmail, caseId, vendorId, _baseUrl));

                _notifyService.Custom($"Case <b> #{policyNumber}</b>  withdrawn successfully", 3, "orange", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing case {Id}. {UserEmail}", caseId, userEmail);
                _notifyService.Error("Error withdrawing case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
        }
    }
}