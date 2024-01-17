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
            IClaimsVendorService vendorService,
            IMailboxService mailboxService,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
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
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }

            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
            }
            var vendorAgent = _context.VendorApplicationUser.FirstOrDefault(c => c.Id.ToString() == selectedcase);

            var claim = await claimsInvestigationService.AssignToVendorAgent(vendorAgent.Email, userEmail, vendorAgent.VendorId.Value, claimId);

            await mailboxService.NotifyClaimAssignmentToVendorAgent(userEmail, claimId, vendorAgent.Email, vendorAgent.VendorId.Value, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] tasked to {1} successfully!", claim.PolicyDetail.ContractNumber, vendorAgent.Email));

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }

        [HttpGet]
        public async Task<IActionResult> PostAgentData(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be investigate.");
                return RedirectToAction(nameof(Index));
            }

            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                toastNotification.AddAlertToastMessage("OOPs !!!..");
                return RedirectToAction(nameof(Index));
            }
            //POST FACE IMAGE AND DOCUMENT
            await vendorService.PostFaceId(userEmail, selectedcase);

            await vendorService.PostDocumentId(userEmail, selectedcase);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Uploaded successfully !"));

            return RedirectToAction(nameof(ClaimsVendorController.GetInvestigate), "ClaimsVendor", new { selectedcase = selectedcase, uploaded = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string remarks, string question1, string question2, string question3, string question4, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(remarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
            {
                toastNotification.AddAlertToastMessage("No Agent remarks entered!!!. Please enter remarks.");
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

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] investigation submitted to supervisor successfully !", claim.PolicyDetail.ContractNumber));

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
                    toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] report submitted to Company successfully !", success.PolicyDetail.ContractNumber));
                }
                else
                {
                    toastNotification.AddSuccessToastMessage("Report sent to review successfully");
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
                toastNotification.AddSuccessToastMessage("report submitted to Company successfully");
            }
            else
            {
                toastNotification.AddSuccessToastMessage("Report sent to review successfully");
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

            toastNotification.AddSuccessToastMessage("Case withdrawn successfully");

            return RedirectToAction(nameof(ClaimsVendorController.Index), "ClaimsVendor");
        }
    }
}