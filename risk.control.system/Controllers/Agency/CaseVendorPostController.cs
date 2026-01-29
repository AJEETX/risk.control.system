using System.Net;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class CaseVendorPostController : Controller
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly IProcessCaseService processCaseService;
        private readonly IVendorInvestigationDetailService vendorInvestigationDetailService;
        private readonly INotyfService notifyService;
        private readonly IMailService mailboxService;
        private readonly ILogger<CaseVendorPostController> logger;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CaseVendorPostController(
            IProcessCaseService processCaseService,
            IVendorInvestigationDetailService vendorInvestigationDetailService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<CaseVendorPostController> logger,
            ApplicationDbContext context)
        {
            this.processCaseService = processCaseService;
            this.vendorInvestigationDetailService = vendorInvestigationDetailService;
            this.notifyService = notifyService;
            this.mailboxService = mailboxService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            this.httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, long claimId)
        {
            try
            {
                if (!ModelState.IsValid || claimId < 1)
                {
                    notifyService.Error($"No case selected!!!. Please select case to be allocate.", 3);
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var vendorAgent = await _context.ApplicationUser.Include(a => a.Vendor).FirstOrDefaultAsync(c => c.Id.ToString() == selectedcase);
                if (vendorAgent == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var claim = await vendorInvestigationDetailService.AssignToVendorAgent(vendorAgent.Email, currentUserEmail, vendorAgent.VendorId.Value, claimId);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error occurred.");
                                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToVendorAgent(currentUserEmail, claimId, vendorAgent.Email, vendorAgent.VendorId.Value, baseUrl));

                notifyService.Custom($"Case <b>#{claim.PolicyDetail.ContractNumber}</b> Tasked to {vendorAgent.Email}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {ClaimId} for {UserName}.", claimId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENT.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> SubmitReport(CaseInvestigationVendorsModel model, string remarks, long claimId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("OOPs !!!..Error in submitting report");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = model.ClaimsInvestigation.Id });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var (vendor, contract) = await vendorInvestigationDetailService.SubmitToVendorSupervisor(currentUserEmail, claimId,
                    WebUtility.HtmlDecode(remarks));
                if (vendor == null)
                {
                    notifyService.Error("OOPs !!!..Error submitting.");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
                }
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseReportSubmitToVendorSupervisor(currentUserEmail, claimId, baseUrl));

                notifyService.Custom($"Case <b> #{contract}</b> report submitted", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(AgentController.Agent), "Agent");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {ClaimId} for {UserName}.", claimId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, long claimId, string remarks, IFormFile? supervisorAttachment)
        {
            try
            {
                if (!ModelState.IsValid || claimId < 1)
                {
                    notifyService.Error("Error in submitting report");
                    return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
                }
                if (supervisorAttachment != null && supervisorAttachment.Length > 0)
                {
                    if (supervisorAttachment.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction("GetInvestigateReport", "VendorInvestigation", new { selectedcase = claimId });
                    }
                    var ext = Path.GetExtension(supervisorAttachment.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        return RedirectToAction("GetInvestigateReport", "VendorInvestigation", new { selectedcase = claimId });
                    }
                    if (!AllowedMime.Contains(supervisorAttachment.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction("GetInvestigateReport", "VendorInvestigation", new { selectedcase = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(supervisorAttachment))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction("GetInvestigateReport", "VendorInvestigation", new { selectedcase = claimId });
                    }
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await processCaseService.ProcessAgentReport(userEmail, supervisorRemarks, claimId, reportUpdateStatus, supervisorAttachment, remarks);

                if (success != null)
                {
                    var agencyUser = await _context.ApplicationUser.Include(a => a.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseReportSubmitToCompany(userEmail, claimId, baseUrl));

                    notifyService.Custom($"Case <b> #{success.PolicyDetail.ContractNumber}</b>  Report submitted to Company", 3, "green", "far fa-file-powerpoint");
                }
                else
                {
                    notifyService.Custom($"Case <b> #{success.PolicyDetail.ContractNumber}</b>  Report sent to review", 3, "orange", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(VendorInvestigationController.CaseReport), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {ClaimId} for {UserName}.", claimId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCase(CaseTransactionModel model, long claimId, string policyNumber)
        {
            try
            {
                if (!ModelState.IsValid || model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                var agency = await processCaseService.WithdrawCase(userEmail, model, claimId);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseWithdrawlToCompany(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case <b> #{policyNumber}</b> Declined successfully", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {ClaimId} for {UserName}.", claimId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCaseFromAgent(CaseTransactionModel model, long claimId, string policyNumber)
        {
            try
            {
                if (!ModelState.IsValid || model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                var agency = await processCaseService.WithdrawCaseFromAgent(userEmail, model, claimId);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseWithdrawlFromAgent(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case <b> #{policyNumber}</b> Withdrawn from Agent successfully", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {ClaimId} for {UserName}.", claimId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> ReplyQuery(long claimId, CaseInvestigationVendorsModel request, IFormFile? document)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                if (document != null && document.Length > 0)
                {
                    if (document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction("ReplyEnquiry", "VendorInvestigation", new { id = claimId });
                    }
                    var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        return RedirectToAction("ReplyEnquiry", "VendorInvestigation", new { id = claimId });
                    }
                    if (!AllowedMime.Contains(document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction("ReplyEnquiry", "VendorInvestigation", new { id = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction("ReplyEnquiry", "VendorInvestigation", new { id = claimId });
                    }
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }

                request.InvestigationReport.EnquiryRequest.DescriptiveAnswer = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveAnswer);

                var claim = await processCaseService.SubmitQueryReplyToCompany(currentUserEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, document);

                if (claim != null)
                {
                    var agencyUser = await _context.ApplicationUser.Include(a => a.Vendor).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitReplyToCompany(currentUserEmail, claimId, baseUrl));

                    notifyService.Success("Enquiry Reply Sent to Company");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {ClaimId} for {UserName}.", claimId, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
        }
    }
}