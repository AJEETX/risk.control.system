﻿using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

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
        private readonly IBackgroundJobClient backgroundJobClient;

        public ClaimsInvestigationPostController(ApplicationDbContext context,
            IClaimsInvestigationService claimsInvestigationService,
            IBackgroundJobClient backgroundJobClient,
            IMailboxService mailboxService,
            INotyfService notifyService)
        {
            _context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.backgroundJobClient = backgroundJobClient;
            this.mailboxService = mailboxService;
            this.notifyService = notifyService;
        }
        
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuto(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var distinctClaims = claims.Distinct().ToList();

                // AUTO ALLOCATION COUNT
                var allocatedClaims = await claimsInvestigationService.UpdateCaseAllocationStatus( currentUserEmail, distinctClaims);

                backgroundJobClient.Enqueue(() => claimsInvestigationService.BackgroundAutoAllocation(distinctClaims, currentUserEmail));
                notifyService.Custom($"Case(s) Assigned(auto) started", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
            }
            return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
        }
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAutoSingle(string claims)
        {
            if (claims == null)
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var allocatedCaseNumber = await claimsInvestigationService.ProcessAutoAllocation(claims, currentUserEmail);
                if(string.IsNullOrWhiteSpace(allocatedCaseNumber))
                {
                    notifyService.Custom($"Case #:{allocatedCaseNumber} Not Assigned", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                }
                notifyService.Custom($"Case #:{allocatedCaseNumber} Assigned", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
        }
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var distinctClaims = claims.Distinct().ToList();
                await claimsInvestigationService.AssignToAssigner(currentUserEmail, distinctClaims);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(currentUserEmail, distinctClaims));

                notifyService.Custom($"{claims.Count}/{claims.Count} case(s) Assigned", 3, "green", "far fa-file-powerpoint");
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
        public async Task<IActionResult> CaseAllocatedToVendor(long selectedcase, string caseId)
        {
            if (selectedcase < 1 || string.IsNullOrWhiteSpace(caseId))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
            }
            //set claim as manual assigned
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var (policy, status) = await claimsInvestigationService.AllocateToVendor(currentUserEmail, caseId, selectedcase, false);

                if(string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
                }

                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAllocationToVendor(currentUserEmail, policy, caseId, selectedcase));
                //await mailboxService.NotifyClaimAllocationToVendor(currentUserEmail, policy.PolicyDetail.ContractNumber, caseId, selectedcase);

                notifyService.Custom($"Case #{policy} {status} to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

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

                var (company, vendorId) = await claimsInvestigationService.WithdrawCaseByCompany(currentUserEmail, model, claimId);
               
                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId, vendorId));
                //await mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId);

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

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId));

                if (reportUpdateStatus == AssessorRemarkType.OK)
                {
                    notifyService.Custom($"Case #{contract} Approved", 3, "green", "far fa-file-powerpoint");
                }
                else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                {
                    notifyService.Custom($"Case #{contract} Rejected", 3, "red", "far fa-file-powerpoint");
                }
                else
                {
                    notifyService.Custom($"Case #{contract} Re-Assigned", 3, "yellow", "far fa-file-powerpoint");
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

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId));

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

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitQueryToAgency(currentUserEmail, claimId));

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