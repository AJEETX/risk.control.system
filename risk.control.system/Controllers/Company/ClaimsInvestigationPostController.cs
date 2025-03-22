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

using System.Diagnostics.Contracts;
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
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == currentUserEmail);

                var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                //IF AUTO ALLOCATION TRUE
                if (company.AutoAllocation)
                {
                    var autoAllocatedClaims = await claimsInvestigationService.ProcessAutoAllocation(claims, company, currentUserEmail);

                    if (claims.Count == autoAllocatedClaims.Count)
                    {
                        notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} case(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                    }

                    else if (claims.Count > autoAllocatedClaims.Count)
                    {
                        if (autoAllocatedClaims.Count > 0)
                        {
                            notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} case(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                        }

                        var notAutoAllocated = claims.Except(autoAllocatedClaims)?.ToList();

                        await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                        await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                        notifyService.Custom($"{notAutoAllocated.Count}/{claims.Count} case(s) need assign manually", 3, "orange", "far fa-file-powerpoint");
                        
                        return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                    }
                }
                else
                {
                    await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

                    await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

                    notifyService.Custom($"{claims.Count}/{claims.Count} case(s) assigned", 3, "green", "far fa-file-powerpoint");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CaseAllocatedToVendor(long selectedcase, string claimId)
        {
            if (selectedcase < 1 || string.IsNullOrWhiteSpace(claimId))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            //set claim as manual assigned
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var policy = await claimsInvestigationService.AllocateToVendor(currentUserEmail, claimId, selectedcase, false);

                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(v => v.Email == currentUserEmail);

                await mailboxService.NotifyClaimAllocationToVendor(currentUserEmail, policy.PolicyDetail.ContractNumber, claimId, selectedcase);

                notifyService.Custom($"Case #{policy.PolicyDetail.ContractNumber} {policy.InvestigationCaseSubStatus.Name} to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

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
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                if (model == null || string.IsNullOrWhiteSpace(claimId))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var company = await claimsInvestigationService.WithdrawCaseByCompany(currentUserEmail, model, claimId);
               
                await mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId);

                notifyService.Custom($"Case #{policyNumber}  withdrawn successfully", 3, "green", "far fa-file-powerpoint");

                if (company.AutoAllocation)
                {
                    if (model.ClaimsInvestigation.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                    }
                }
                else 
                {
                    return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, string reportAiSummary)
        {
            if (string.IsNullOrWhiteSpace(assessorRemarks) || string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                AssessorRemarkType reportUpdateStatus = (AssessorRemarkType)Enum.Parse(typeof(AssessorRemarkType), assessorRemarkType, true);

                var (company, contract) = await claimsInvestigationService.ProcessCaseReport(currentUserEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                await mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId);

                if (reportUpdateStatus == AssessorRemarkType.OK)
                {
                    notifyService.Custom($"Case #{contract} report approved", 3, "green", "far fa-file-powerpoint");
                }
                else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                {
                    notifyService.Custom($"Case #{contract} rejected", 3, "red", "far fa-file-powerpoint");
                }
                else
                {
                    notifyService.Custom($"Case #{contract} reassigned", 3, "yellow", "far fa-file-powerpoint");
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
        public async Task<IActionResult> ReProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, string reportAiSummary)
        {
            if (string.IsNullOrWhiteSpace(assessorRemarks) || string.IsNullOrWhiteSpace(claimId))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var reportUpdateStatus = AssessorRemarkType.REVIEW;

                var (company, contract) = await claimsInvestigationService.ProcessCaseReport(currentUserEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                await mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId);

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
                if (request == null || string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(reply))
                {
                    notifyService.Error("Bad Request..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                request.AgencyReport.EnquiryRequest.Description = HttpUtility.HtmlEncode(request.AgencyReport.EnquiryRequest.Description);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

                var model = await claimsInvestigationService.SubmitQueryToAgency(currentUserEmail, claimId, request.AgencyReport.EnquiryRequest, messageDocument);
                if (model != null)
                {
                    var company = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);

                    await mailboxService.NotifySubmitQueryToAgency(currentUserEmail, claimId);

                    notifyService.Success("Query Sent to Agency");
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
                notifyService.Error("OOPs !!!..Error sending query");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> SubmitNotes(string claimId, string name)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var model = await claimsInvestigationService.SubmitNotes(currentUserEmail, claimId, name);
                if (model)
                {
                    notifyService.Success("Notes added");
                    return Ok();
                }
                notifyService.Error("OOPs !!!..Error adding notes");
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return Ok();
            }
        }
    }
}