using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace risk.control.system.Controllers
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
        private readonly IFtpService ftpService;
        private readonly IHttpClientService httpClientService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IClaimPolicyService claimPolicyService;
        private static HttpClient httpClient = new();

        public ClaimsInvestigationPostController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IFtpService ftpService,
            IHttpClientService httpClientService,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            UserManager<ClientCompanyApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            RoleManager<ApplicationRole> roleManager,
            INotyfService notifyService,

            IClaimPolicyService claimPolicyService,
            IToastNotification toastNotification)
        {
            _context = context;
            this.ftpService = ftpService;
            this.httpClientService = httpClientService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
            this.roleManager = roleManager;
            this.notifyService = notifyService;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.toastNotification = toastNotification;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(ClaimsInvestigationController.Draft), "ClaimsInvestigation");
            }

            //IF AUTO ALLOCATION TRUE
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(e => e.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(e => e.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId);

            if (company is not null && company.AutoAllocation)
            {
                var autoAllocatedClaims = await claimsInvestigationService.ProcessAutoAllocation(claims, company, userEmail);

                if (claims.Count == autoAllocatedClaims.Count)
                {
                    notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                }

                if (claims.Count > autoAllocatedClaims.Count)
                {
                    if (autoAllocatedClaims.Count > 0)
                    {
                        notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                    }

                    var notAutoAllocated = claims.Except(autoAllocatedClaims)?.ToList();

                    await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                    await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                    notifyService.Custom($"{notAutoAllocated.Count}/{claims.Count} claim(s) need manual assign", 3, "orange", "far fa-file-powerpoint");

                    return RedirectToAction(nameof(ClaimsInvestigationController.Assigner), "ClaimsInvestigation");
                }
            }
            else
            {
                await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

                await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

                notifyService.Custom($"{claims.Count}/{claims.Count} claim(s) assigned", 3, "green", "far fa-file-powerpoint");
            }

            return RedirectToAction(nameof(ClaimsInvestigationController.Draft), "ClaimsInvestigation");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CaseAllocatedToVendor(long selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            var policy = await claimsInvestigationService.AllocateToVendor(userEmail, claimId, selectedcase, caseLocationId);

            await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimId, selectedcase, caseLocationId);

            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

            notifyService.Custom($"Policy #{policy.PolicyDetail.ContractNumber} assigned to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

            return RedirectToAction(nameof(ClaimsInvestigationController.Assigner), "ClaimsInvestigation");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            AssessorRemarkType reportUpdateStatus = (AssessorRemarkType)Enum.Parse(typeof(AssessorRemarkType), assessorRemarkType, true);

            var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} report approved", 3, "green", "far fa-file-powerpoint");
            }
            else
            {
                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} reassigned", 3, "red", "far fa-file-powerpoint");
            }

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor), "ClaimsInvestigation");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ReProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            if (string.IsNullOrWhiteSpace(assessorRemarks))
            {
                assessorRemarks = "review";
            }
            var reportUpdateStatus = AssessorRemarkType.REVIEW;

            var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor), "ClaimsInvestigation");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation claimsInvestigation)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");

            return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = claim.ClaimsInvestigationId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.EdiPolicy(userEmail, claimsInvestigation, documentFile);

            notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} edited successfully", 3, "orange", "far fa-file-powerpoint");

            return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = claim.ClaimsInvestigationId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.CreateCustomer(userEmail, claimsInvestigation, documentFile, profileFile, create);

            notifyService.Custom($"Customer {claim.CustomerDetail.CustomerName} added successfully", 3, "green", "fas fa-user-plus");

            return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = claim.ClaimsInvestigationId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claim = await claimsInvestigationService.EditCustomer(userEmail, claimsInvestigation, profileFile);

            notifyService.Custom($"Customer {claim.CustomerDetail.CustomerName} edited successfully", 3, "orange", "fas fa-user-plus");

            return RedirectToAction(nameof(ClaimsInvestigationController.Details), "ClaimsInvestigation", new { id = claim.ClaimsInvestigationId });
        }
    }
}