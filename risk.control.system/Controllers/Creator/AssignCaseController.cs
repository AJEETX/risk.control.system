using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Creator
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
    public class AssignCaseController : Controller
    {
        private readonly string baseUrl;
        private readonly ApplicationDbContext _context;
        private readonly IProcessCaseService processCaseService;
        private readonly IMailService mailboxService;
        private readonly INotyfService notifyService;
        private readonly ILogger<AssignCaseController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public AssignCaseController(ApplicationDbContext context,
            IProcessCaseService processCaseService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AssignCaseController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            this.processCaseService = processCaseService;
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
        public async Task<IActionResult> AssignAuto(List<long> claims)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || claims == null || claims.Count == 0)
                {
                    notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                // AUTO ALLOCATION COUNT
                var distinctClaims = claims.Distinct().ToList();
                var affectedRows = await processCaseService.UpdateCaseAllocationStatus(userEmail, distinctClaims);
                if (affectedRows < distinctClaims.Count)
                {
                    notifyService.Custom($"Case(s) assignment error", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }
                var jobId = backgroundJobClient.Enqueue(() => processCaseService.BackgroundAutoAllocation(distinctClaims, userEmail, baseUrl));
                notifyService.Custom($"Assignment of <b> {distinctClaims.Count}</b> Case(s) started", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive", new { jobId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting case(s). {UserEmail}", userEmail);
                notifyService.Error("Error assigning case. Try again.");
            }
            return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
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
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var (policy, status) = await processCaseService.AllocateToVendor(userEmail, caseId, selectedcase, false);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == selectedcase);

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAllocationToVendorAndManager(userEmail, policy, caseId, selectedcase, baseUrl));

                notifyService.Custom($"Case <b>#{policy}</b> <i>{status}</i> to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning case {Id} to {Agency}. {UserEmail}", caseId, selectedcase, userEmail);
                notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
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
                    notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var allocatedCaseNumber = await processCaseService.ProcessAutoSingleAllocation(claims, userEmail, baseUrl);
                if (string.IsNullOrWhiteSpace(allocatedCaseNumber))
                {
                    notifyService.Custom($"Case #:{allocatedCaseNumber} Not Assigned", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }
                notifyService.Custom($"Case <b>#:{allocatedCaseNumber}</b> Assigned<sub>auto</b>", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning case {Id}. {UserEmail}", claims, userEmail);
                notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
            return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive");
        }
    }
}