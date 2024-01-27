using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ClaimsVendorPostController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly INotyfService notifyService;
        private readonly IClaimsVendorService vendorService;
        private readonly IMailboxService mailboxService;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static HttpClient httpClient = new();

        public ClaimsVendorPostController(
            IClaimsInvestigationService claimsInvestigationService,
            UserManager<VendorApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IDashboardService dashboardService,
            INotyfService notifyService,
            IClaimsVendorService vendorService,
            IMailboxService mailboxService,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.mailboxService = mailboxService;
            this.toastNotification = toastNotification;
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(selectedcase) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                notifyService.Error($"No case selected!!!. Please select case to be allocate.", 3);
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }

            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                notifyService.Error($"OOPs !!!..Err", 3);
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }
            var vendorAgent = _context.VendorApplicationUser.FirstOrDefault(c => c.Id.ToString() == selectedcase);

            var claim = await claimsInvestigationService.AssignToVendorAgent(vendorAgent.Email, userEmail, vendorAgent.VendorId.Value, claimId);

            await mailboxService.NotifyClaimAssignmentToVendorAgent(userEmail, claimId, vendorAgent.Email, vendorAgent.VendorId.Value, caseLocationId);

            notifyService.Custom($"Claim #{claim.PolicyDetail.ContractNumber} tasked to {vendorAgent.Email}", 3, "green", "far fa-file-powerpoint");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string remarks, string question1, string question2, string question3, string question4, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(remarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                notifyService.Error($"No Agent remarks entered!!!. Please enter remarks.", 3);
                return RedirectToAction(nameof(ClaimsVendorController.GetInvestigate), "\"ClaimsVendor\"", new { selectedcase = claimId });
            }

            string userEmail = HttpContext?.User?.Identity.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }

            //END : POST FACE IMAGE AND DOCUMENT

            var claim = await claimsInvestigationService.SubmitToVendorSupervisor(userEmail, caseLocationId, claimId, remarks, question1, question2, question3, question4);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(userEmail, claimId, caseLocationId);

            notifyService.Custom($"Claim #{claim.PolicyDetail.ContractNumber}  report submitted to supervisor", 3, "green", "far fa-file-powerpoint");

            return RedirectToAction(nameof(ClaimsVendorController.Agent), "ClaimsVendor");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
                {
                    toastNotification.AddAlertToastMessage("No Supervisor remarks entered!!!. Please enter remarks.");
                    return RedirectToAction(nameof(ClaimsVendorController.GetInvestigateReport), new { selectedcase = claimId });
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    toastNotification.AddAlertToastMessage("OOPs !!!..");
                    return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
                }

                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await claimsInvestigationService.ProcessAgentReport(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus);

                if (success != null)
                {
                    await mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId, caseLocationId);
            notifyService.Custom($"Claim #{success.PolicyDetail.ContractNumber}  report submitted to Company", 3, "green", "far fa-file-powerpoint");
                }
                else
                {
            notifyService.Custom($"Claim #{success.PolicyDetail.ContractNumber}  report sent to review", 3, "orange", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(ClaimsVendorController.ClaimReport), "ClaimsVendor");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReAllocateReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No remarks entered!!!. Please enter remarks.");
                return RedirectToAction(nameof(ClaimsVendorController.GetInvestigate), new { selectedcase = claimId });
            }
            string userEmail = HttpContext?.User?.Identity.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }

            var reportUpdateStatus = SupervisorRemarkType.REVIEW;

            var success = await claimsInvestigationService.ProcessAgentReport(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus);

            if (success != null)
            {
                await mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId, caseLocationId);
            notifyService.Custom($"Claim #{success.PolicyDetail.ContractNumber}  report sent to review", 3, "green", "far fa-file-powerpoint");
            }
            else
            {
            notifyService.Custom($"Claim #{success.PolicyDetail.ContractNumber}  report sent to review", 3, "orange", "far fa-file-powerpoint");
            }
            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCase(ClaimTransactionModel model, string claimId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }
            await claimsInvestigationService.WithdrawCase(userEmail, model, claimId);

            await mailboxService.NotifyClaimWithdrawlToCompany(userEmail, claimId);

            notifyService.Custom($"Claim #{model.ClaimsInvestigation.PolicyDetail.ContractNumber}  withdrawn successfully", 3, "green", "far fa-file-powerpoint");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }
    }
}