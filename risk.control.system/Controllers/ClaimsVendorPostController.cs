using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Extensions;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ClaimsVendorPostController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly INotyfService notifyService;
        private readonly IClaimsVendorService vendorService;
        private readonly IMailboxService mailboxService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ClaimsVendorPostController(
            IClaimsInvestigationService claimsInvestigationService,
            IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IClaimsVendorService vendorService,
            IMailboxService mailboxService,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.mailboxService = mailboxService;
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, string claimId, long caseLocationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
                {
                    notifyService.Error($"No case selected!!!. Please select case to be allocate.", 3);
                    return RedirectToAction(nameof(ClaimsVendorController.Allocate), "ClaimsVendor");
                }

                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorAgent = _context.VendorApplicationUser.FirstOrDefault(c => c.Id.ToString() == selectedcase);
                if(vendorAgent == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claim = await claimsInvestigationService.AssignToVendorAgent(vendorAgent.Email, userEmail, vendorAgent.VendorId.Value, claimId);
                if(claim == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                await mailboxService.NotifyClaimAssignmentToVendorAgent(userEmail, claimId, vendorAgent.Email, vendorAgent.VendorId.Value, caseLocationId);

                notifyService.Custom($"Claim #{claim.PolicyDetail.ContractNumber} tasked to {vendorAgent.Email}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(ClaimsVendorController.Allocate), "ClaimsVendor");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string remarks, string question1, string question2, string question3, string question4, string claimId, long caseLocationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(remarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
                {
                    notifyService.Error($"No Agent remarks entered!!!. Please enter remarks.", 3);
                    return RedirectToAction(nameof(ClaimsVendorController.GetInvestigate), "\"ClaimsVendor\"", new { selectedcase = claimId });
                }

                string userEmail = HttpContext?.User?.Identity.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                //END : POST FACE IMAGE AND DOCUMENT

                if (!string.IsNullOrWhiteSpace(question1))
                {
                    DwellType question1Enum = (DwellType)Enum.Parse(typeof(DwellType), question1, true);
                    question1 = question1Enum.GetEnumDisplayName();
                }

                if (!string.IsNullOrWhiteSpace(question2))
                {
                    Income question2Enum = (Income)Enum.Parse(typeof(Income), question2, true);
                    question2 = question2Enum.GetEnumDisplayName();
                }
                var claim = await claimsInvestigationService.SubmitToVendorSupervisor(userEmail, caseLocationId, claimId, remarks, question1, question2, question3, question4);
                if(claim == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(userEmail, claimId, caseLocationId);

                notifyService.Custom($"Claim #{claim.PolicyDetail.ContractNumber}  report submitted to supervisor", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(ClaimsVendorController.Agent), "ClaimsVendor");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, string claimId, long caseLocationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId) || caseLocationId < 1)
                {
                    notifyService.Error("No Supervisor remarks entered!!!. Please enter remarks.");
                    return RedirectToAction(nameof(ClaimsVendorController.GetInvestigateReport), new { selectedcase = claimId });
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await claimsInvestigationService.ProcessAgentReport(userEmail, supervisorRemarks, caseLocationId, claimId, reportUpdateStatus, Request.Form.Files.FirstOrDefault());

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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCase(ClaimTransactionModel model, string claimId, string policyNumber)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(claimId))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                await claimsInvestigationService.WithdrawCase(userEmail, model, claimId);

                await mailboxService.NotifyClaimWithdrawlToCompany(userEmail, claimId);

                notifyService.Custom($"Claim #{policyNumber}  declined successfully", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction(nameof(ClaimsVendorController.Allocate), "ClaimsVendor");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyQuery(string claimId, ClaimsInvestigationVendorsModel request, List<string> flexRadioDefault)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (request == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }
                request.ClaimsInvestigation.AgencyReport.EnquiryRequest.Answer = HttpUtility.HtmlEncode(request.ClaimsInvestigation.AgencyReport.EnquiryRequest.Answer);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

                var claim = await claimsInvestigationService.SubmitQueryReplyToCompany(currentUserEmail, claimId, request.ClaimsInvestigation.AgencyReport.EnquiryRequest, messageDocument, flexRadioDefault);

                if (claim != null)
                {
                    notifyService.Success("Enquiry Reply Sent to Company");
                    return RedirectToAction(nameof(ClaimsVendorController.Allocate), "ClaimsVendor");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}