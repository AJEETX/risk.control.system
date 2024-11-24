using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;
using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Company
{
    public class ClaimsInvestigationPostController : Controller
    {
        private readonly JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly INotyfService notifyService;

        public ClaimsInvestigationPostController(ApplicationDbContext context,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            INotyfService notifyService)
        {
            _context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.notifyService = notifyService;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            try
            {
                var userEmail = HttpContext.User.Identity.Name;
                var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

                var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.PincodeServices)
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId);

                //IF AUTO ALLOCATION TRUE
                if (company.AutoAllocation)
                {
                    var autoAllocatedClaims = await claimsInvestigationService.ProcessAutoAllocation(claims, company, userEmail);

                    if (claims.Count == autoAllocatedClaims.Count)
                    {
                        notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                        return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");

                    }

                    else if (claims.Count > autoAllocatedClaims.Count)
                    {
                        if (autoAllocatedClaims.Count > 0)
                        {
                            notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                        }

                        var notAutoAllocated = claims.Except(autoAllocatedClaims)?.ToList();

                        await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                        await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                        notifyService.Custom($"{notAutoAllocated.Count}/{claims.Count} claim(s) need assign manually", 3, "orange", "far fa-file-powerpoint");

                        return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");

                    }
                }
                else
                {
                    await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

                    await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

                    notifyService.Custom($"{claims.Count}/{claims.Count} claim(s) assigned", 3, "green", "far fa-file-powerpoint");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");

            }

                        return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CaseAllocatedToVendor(long selectedcase, string claimId, long caseLocationId)
        {
            if (selectedcase < 1 || caseLocationId < 1 || string.IsNullOrWhiteSpace(claimId))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            //set claim as manual assigned
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;

                var policy = await claimsInvestigationService.AllocateToVendor(userEmail, claimId, selectedcase, caseLocationId, false);

                await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimId, selectedcase, caseLocationId);

                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

                notifyService.Custom($"Policy #{policy.PolicyDetail.ContractNumber} assigned to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

                        return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");

            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
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

                var isAutoAllocationOn = await claimsInvestigationService.WithdrawCaseByCompany(userEmail, model, claimId);

                await mailboxService.NotifyClaimWithdrawlToCompany(userEmail, claimId);
                notifyService.Custom($"Claim #{policyNumber}  withdrawn successfully", 3, "green", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(assessorRemarks) || caseLocationId < 1 || string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            try
            {
                string userEmail = HttpContext?.User?.Identity.Name;

                AssessorRemarkType reportUpdateStatus = (AssessorRemarkType)Enum.Parse(typeof(AssessorRemarkType), assessorRemarkType, true);

                var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

                await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

                if (reportUpdateStatus == AssessorRemarkType.OK)
                {
                    notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} report approved", 3, "green", "far fa-file-powerpoint");
                }
                else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                {
                    notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} rejected", 3, "red", "far fa-file-powerpoint");
                }
                else
                {
                    notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} reassigned", 3, "yellow", "far fa-file-powerpoint");
                }

                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ReProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            if (string.IsNullOrWhiteSpace(assessorRemarks) || caseLocationId < 1 || string.IsNullOrWhiteSpace(claimId))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            try
            {
                string userEmail = HttpContext?.User?.Identity.Name;

                var reportUpdateStatus = AssessorRemarkType.REVIEW;

                var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

                await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> SubmitQuery(string claimId, string reply, ClaimsInvestigationVendorsModel request)
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
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                request.AgencyReport.EnquiryRequest.Description = HttpUtility.HtmlEncode(request.AgencyReport.EnquiryRequest.Description);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

                var model = await claimsInvestigationService.SubmitQueryToAgency(currentUserEmail, claimId, request.AgencyReport.EnquiryRequest, messageDocument);
                if (model != null)
                {
                    await mailboxService.NotifySubmitQueryToAgency(currentUserEmail, claimId);

                    notifyService.Success("Query Sent to Agency");
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}