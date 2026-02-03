using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    public class WithdrawCaseController : Controller
    {
        private readonly string baseUrl;
        private readonly ApplicationDbContext _context;
        private readonly IWithdrawCaseService withdrawCaseService;
        private readonly IMailService mailboxService;
        private readonly INotyfService notifyService;
        private readonly ILogger<WithdrawCaseController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public WithdrawCaseController(ApplicationDbContext context,
            IWithdrawCaseService withdrawCaseService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WithdrawCaseController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            this.withdrawCaseService = withdrawCaseService;
            this.mailboxService = mailboxService;
            this.notifyService = notifyService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> Withdraw(CaseTransactionModel model, long claimId, string policyNumber)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var (company, vendorId) = await withdrawCaseService.WithdrawCaseByCompany(currentUserEmail, model, claimId);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseWithdrawlToCompany(currentUserEmail, claimId, vendorId, baseUrl));

                notifyService.Custom($"Case <b> #{policyNumber}</b>  withdrawn successfully", 3, "orange", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error withdrawing case {Id}. {UserEmail}", claimId, currentUserEmail);
                notifyService.Error("Error withdrawing case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
        }
    }
}