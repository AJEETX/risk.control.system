using AspNetCoreHero.ToastNotification.Abstractions;

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
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProgressService progressService;
        private readonly INotyfService notifyService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public ClaimsInvestigationPostController(ApplicationDbContext context,
            IClaimsInvestigationService claimsInvestigationService,
            IBackgroundJobClient backgroundJobClient,
            IMailboxService mailboxService,
            IHttpContextAccessor httpContextAccessor,
            IProgressService progressService,
            INotyfService notifyService)
        {
            _context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.backgroundJobClient = backgroundJobClient;
            this.mailboxService = mailboxService;
            this.httpContextAccessor = httpContextAccessor;
            this.progressService = progressService;
            this.notifyService = notifyService;
        }
        
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuto(List<string> claims)
        {
            
            try
            {
                if (claims == null || claims.Count == 0)
                {
                    notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                }
                
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
                
                // AUTO ALLOCATION COUNT
                var distinctClaims = claims.Distinct().ToList();
                var affectedRows = await claimsInvestigationService.UpdateCaseAllocationStatus( currentUserEmail, distinctClaims);
                if(affectedRows <= distinctClaims.Count)
                {
                    notifyService.Custom($"Case(s) assignment error", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                }
                var jobId = backgroundJobClient.Enqueue(() => claimsInvestigationService.BackgroundAutoAllocation(distinctClaims, currentUserEmail, baseUrl));
                progressService.AddAssignmentJob(jobId, currentUserEmail);
                notifyService.Custom($"Assignment of {distinctClaims.Count} Case(s) started", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive",new { jobId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
            }
            return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
        }


        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAutoSingle(string claims)
        {
            if (claims == null || string.IsNullOrWhiteSpace(claims))
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var allocatedCaseNumber = await claimsInvestigationService.ProcessAutoAllocation(claims, currentUserEmail, baseUrl);
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
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
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
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                await claimsInvestigationService.AssignToAssigner(currentUserEmail, distinctClaims,baseUrl);

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(currentUserEmail, distinctClaims,baseUrl));

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
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            //set claim as manual assigned
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var (policy, status) = await claimsInvestigationService.AllocateToVendor(currentUserEmail, caseId, selectedcase, false);

                if(string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                }

                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAllocationToVendor(currentUserEmail, policy, caseId, selectedcase, baseUrl));

                notifyService.Custom($"Case #{policy} {status} to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
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
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId, vendorId, baseUrl));
                //await mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId);

                notifyService.Custom($"Case #{policyNumber}  withdrawn successfully", 3, "green", "far fa-file-powerpoint");

                    return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
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

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";


                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId, baseUrl));

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
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId, baseUrl));

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
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitQueryToAgency(currentUserEmail, claimId, baseUrl));

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