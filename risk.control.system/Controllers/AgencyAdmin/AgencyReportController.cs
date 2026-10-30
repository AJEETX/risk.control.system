using System.Net;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Agent;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Agentic;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{AGENT.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyReportController(
            IProcessSubmittedReportService processSubmittedReportService,
            IAgenticService agenticService,
            IEncryptionService encryptionService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            ICaseNotificationService notoficationService,
            ILogger<AgencyReportController> logger) : Controller
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly IProcessSubmittedReportService _processSubmittedReportService = processSubmittedReportService;
        private readonly IAgenticService _agenticService = agenticService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly ICaseNotificationService _notoficationService = notoficationService;
        private readonly ILogger<AgencyReportController> _logger = logger;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(CaseAgencyModel model, string remarks, long caseId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("OOPs !!!..Error in submitting report");
                return RedirectToAction(nameof(AgentController.Agent), ControllerName<AgentController>.Name);
            }

            try
            {
                var (vendor, contract) = await _processSubmittedReportService.SubmitToVendorSupervisor(userEmail!, caseId, WebUtility.HtmlDecode(remarks));
                if (vendor == null)
                {
                    _notifyService.Error("OOPs !!!..Error submitting.");
                    var encryptedId = _encryptionService.ProtectId(caseId);
                    return RedirectToAction(nameof(AgentController.GetInvestigate), ControllerName<AgentController>.Name, new { id = encryptedId });
                }
                _backgroundJobClient.Enqueue(() => _notoficationService.NotifyCaseReportSubmitToVendorSupervisor(userEmail!, caseId, _baseUrl));

                _notifyService.Custom($"Case <b> #{contract}</b> report submitted", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(AgentController.Agent), ControllerName<AgentController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for CaseId {Id}. {UserEmail}.", caseId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                var encryptedId = _encryptionService.ProtectId(caseId);
                return RedirectToAction(nameof(AgentController.GetInvestigate), ControllerName<AgentController>.Name, new { id = encryptedId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, long claimId, string remarks, IFormFile? supervisorAttachment)
        {
            string userEmail = HttpContext?.User?.Identity?.Name!;
            if (!ModelState.IsValid || claimId < 1)
            {
                _notifyService.Error("Error in submitting report");
                return RedirectToAction(nameof(VendorInvestigationController.ReportDetail), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
            }
            var encryptedId = _encryptionService.ProtectId(claimId);

            try
            {
                if (supervisorAttachment != null && supervisorAttachment.Length > 0)
                {
                    if (supervisorAttachment.Length > MAX_FILE_SIZE)
                    {
                        _notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction(nameof(VendorInvestigationController.ReportDetail), ControllerName<VendorInvestigationController>.Name, new { selectedcase = encryptedId });
                    }
                    var ext = Path.GetExtension(supervisorAttachment.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        _notifyService.Error($"Invalid Document image type");
                        return RedirectToAction(nameof(VendorInvestigationController.ReportDetail), ControllerName<VendorInvestigationController>.Name, new { selectedcase = encryptedId });
                    }
                    if (!AllowedMime.Contains(supervisorAttachment.ContentType))
                    {
                        _notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction(nameof(VendorInvestigationController.ReportDetail), ControllerName<VendorInvestigationController>.Name, new { selectedcase = encryptedId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(supervisorAttachment))
                    {
                        _notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction(nameof(VendorInvestigationController.ReportDetail), ControllerName<VendorInvestigationController>.Name, new { selectedcase = encryptedId });
                    }
                }
                var caseTask = await _processSubmittedReportService.ProcessAgentReport(userEmail, supervisorRemarks, claimId, SupervisorRemarkType.OK, supervisorAttachment, remarks);

                _backgroundJobClient.Enqueue(() => _notoficationService.NotifyCaseReportSubmitToCompany(userEmail, claimId, _baseUrl));

                _notifyService.Custom($"Case <b> #{caseTask.PolicyDetail!.ContractNumber}</b>  Report submitted to Company", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.AgentReport), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.ReportDetail), ControllerName<VendorInvestigationController>.Name, new { selectedcase = encryptedId });
            }
        }
    }
}