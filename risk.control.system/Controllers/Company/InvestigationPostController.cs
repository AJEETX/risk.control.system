using System.Net;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using risk.control.system.AppConstant;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class InvestigationPostController : Controller
    {
        private readonly string baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly ApplicationDbContext _context;
        private readonly IProcessCaseService processCaseService;
        private readonly IMailService mailboxService;
        private readonly INotyfService notifyService;
        private readonly ILogger<InvestigationPostController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public InvestigationPostController(ApplicationDbContext context,
            IProcessCaseService processCaseService,
            IMailService mailboxService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<InvestigationPostController> logger,
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

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(long id)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request." });
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                if (id <= 0)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var claimsInvestigation = await _context.Investigations.FindAsync(id);
                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.Investigations.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Case deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting case {Id}. {UserEmail}", id, currentUserEmail);
                notifyService.Error("Error deleting case. Try again.");
                return Json(new { success = false, message = "Error deleting case. Try again." });
            }
        }

        [HttpPost, ActionName("DeleteCases")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCases([FromBody] DeleteRequestModel request)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid request." });
                }
                if (request.claims == null || request.claims.Count == 0)
                {
                    return Json(new { success = false, message = "No cases selected for deletion." });
                }

                foreach (var claim in request.claims)
                {
                    var claimsInvestigation = await _context.Investigations.FindAsync(claim);
                    if (claimsInvestigation == null)
                    {
                        notifyService.Error("Not Found!!!..Contact Admin");
                                        return this.RedirectToAction<DashboardController>(x => x.Index());
                    }

                    claimsInvestigation.Updated = DateTime.Now;
                    claimsInvestigation.UpdatedBy = currentUserEmail;
                    claimsInvestigation.Deleted = true;
                    _context.Investigations.Update(claimsInvestigation);
                }
                var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting cases. {UserEmail}", currentUserEmail);
                notifyService.Error("Error deleting cases. Try again.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuto(List<long> claims)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || claims == null || claims.Count == 0)
                {
                    notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                // AUTO ALLOCATION COUNT
                var distinctClaims = claims.Distinct().ToList();
                var affectedRows = await processCaseService.UpdateCaseAllocationStatus(currentUserEmail, distinctClaims);
                if (affectedRows < distinctClaims.Count)
                {
                    notifyService.Custom($"Case(s) assignment error", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }
                var jobId = backgroundJobClient.Enqueue(() => processCaseService.BackgroundAutoAllocation(distinctClaims, currentUserEmail, baseUrl));
                notifyService.Custom($"Assignment of <b> {distinctClaims.Count}</b> Case(s) started", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive", new { jobId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting cases. {UserEmail}", currentUserEmail);
                notifyService.Error("Error assigning case. Try again.");
            }
            return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> AllocateSingle2Vendor(long selectedcase, long caseId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1 || caseId < 1)
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var (policy, status) = await processCaseService.AllocateToVendor(currentUserEmail, caseId, selectedcase, false);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == selectedcase);

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAllocationToVendorAndManager(currentUserEmail, policy, caseId, selectedcase, baseUrl));

                notifyService.Custom($"Case <b>#{policy}</b> <i>{status}</i> to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning case {Id} to {Agency}. {UserEmail}", selectedcase, caseId, currentUserEmail);
                notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
        }

        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAutoSingle(long claims)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || claims < 1)
                {
                    notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var allocatedCaseNumber = await processCaseService.ProcessAutoSingleAllocation(claims, currentUserEmail, baseUrl);
                if (string.IsNullOrWhiteSpace(allocatedCaseNumber))
                {
                    notifyService.Custom($"Case #:{allocatedCaseNumber} Not Assigned", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }
                notifyService.Custom($"Case <b>#:{allocatedCaseNumber}</b> Assigned<sub>auto</b>", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning cases. {UserEmail}", currentUserEmail);
                notifyService.Error("Error assigning case. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
            return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> WithdrawCase(CaseTransactionModel model, long claimId, string policyNumber)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var (company, vendorId) = await processCaseService.WithdrawCaseByCompany(currentUserEmail, model, claimId);

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

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, long claimId, string reportAiSummary = "")
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(assessorRemarks) || claimId < 1 || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (Enum.TryParse<AssessorRemarkType>(assessorRemarkType, true, out var reportUpdateStatus))
                {
                    assessorRemarks = WebUtility.HtmlEncode(assessorRemarks);
                    reportAiSummary = WebUtility.HtmlEncode(reportAiSummary);

                    var (company, contract) = await processCaseService.ProcessCaseReport(currentUserEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                    backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseReportProcess(currentUserEmail, claimId, baseUrl));
                    if (reportUpdateStatus == AssessorRemarkType.OK)
                    {
                        notifyService.Custom($"Case <b> #{contract}</b> Approved", 3, "green", "far fa-file-powerpoint");
                    }
                    else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                    {
                        notifyService.Custom($"Case <b>#{contract}</b> Rejected", 3, "red", "far fa-file-powerpoint");
                    }
                    else
                    {
                        notifyService.Custom($"Case <b> #{contract}</b> Re-Assigned", 3, "yellow", "far fa-file-powerpoint");
                    }
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
                else
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error withdrawing case {Id}. {UserEmail}", claimId, currentUserEmail);
                notifyService.Error("Error processing case. Try again.");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> SubmitQuery(long claimId, string reply, CaseInvestigationVendorsModel request, IFormFile? document)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Bad Request..");
                    return RedirectToAction("SendEnquiry", "Assessor", new { selectedcase = claimId });
                }
                if (document != null && document.Length > 0)
                {
                    if (document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction("SendEnquiry", "Assessor", new { selectedcase = claimId });
                    }
                    var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        return RedirectToAction("SendEnquiry", "Assessor", new { selectedcase = claimId });
                    }
                    if (!AllowedMime.Contains(document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction("SendEnquiry", "Assessor", new { selectedcase = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction("SendEnquiry", "Assessor", new { selectedcase = claimId });
                    }
                }

                request.InvestigationReport.EnquiryRequest.DescriptiveQuestion = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveQuestion);

                var model = await processCaseService.SubmitQueryToAgency(currentUserEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, document);
                if (model != null)
                {
                    var company = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitQueryToAgency(currentUserEmail, claimId, baseUrl));

                    notifyService.Success("Enquiry Sent to Agency");
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
                notifyService.Error("OOPs !!!..Error sending query");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error submitting query case {Id}", claimId, currentUserEmail);
                notifyService.Error("Error submitting query. Try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> SubmitNotes(long claimId, string name)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || claimId < 1 || string.IsNullOrWhiteSpace(name))
                {
                    notifyService.Error("Bad Request..");
                    return Ok();
                }

                var model = await processCaseService.SubmitNotes(currentUserEmail, claimId, name);
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
                logger.LogError(ex, "Error submitting notes case {Id}", claimId, currentUserEmail);
                notifyService.Error("Error submitting notes. Try again.");
                return StatusCode(500);
            }
        }
    }
}