using System.Net;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{AGENT.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyReportController : Controller
    {
        private readonly string baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private IProcessSubmittedReportService processSubmittedReportService;
        private readonly INotyfService notifyService;
        private readonly IMailService mailboxService;
        private readonly ILogger<AgencyReportController> logger;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient backgroundJobClient;

        public AgencyReportController(
            IProcessSubmittedReportService processSubmittedReportService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<AgencyReportController> logger,
            ApplicationDbContext context)
        {
            this.processSubmittedReportService = processSubmittedReportService;
            this.notifyService = notifyService;
            this.mailboxService = mailboxService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(CaseInvestigationVendorsModel model, string remarks, long claimId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPs !!!..Error in submitting report");
                return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = model.ClaimsInvestigation.Id });
            }

            try
            {
                var (vendor, contract) = await processSubmittedReportService.SubmitToVendorSupervisor(userEmail, claimId,
                    WebUtility.HtmlDecode(remarks));
                if (vendor == null)
                {
                    notifyService.Error("OOPs !!!..Error submitting.");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
                }
                backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseReportSubmitToVendorSupervisor(userEmail, claimId, baseUrl));

                notifyService.Custom($"Case <b> #{contract}</b> report submitted", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(AgentController.Agent), "Agent");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for CaseId {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, long claimId, string remarks, IFormFile? supervisorAttachment)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            if (!ModelState.IsValid || claimId < 1)
            {
                notifyService.Error("Error in submitting report");
                return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
            }
            try
            {
                if (supervisorAttachment != null && supervisorAttachment.Length > 0)
                {
                    if (supervisorAttachment.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
                    }
                    var ext = Path.GetExtension(supervisorAttachment.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
                    }
                    if (!AllowedMime.Contains(supervisorAttachment.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(supervisorAttachment))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
                    }
                }
                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await processSubmittedReportService.ProcessAgentReport(userEmail, supervisorRemarks, claimId, reportUpdateStatus, supervisorAttachment, remarks);

                if (success != null)
                {
                    var agencyUser = await _context.ApplicationUser.Include(a => a.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);

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
                logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), "VendorInvestigation", new { selectedcase = claimId });
            }
        }
    }
}