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
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{AGENT.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyReportController : Controller
    {
        private readonly string _baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private IProcessSubmittedReportService _processSubmittedReportService;
        private readonly INotyfService _notifyService;
        private readonly IMailService _mailService;
        private readonly ILogger<AgencyReportController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AgencyReportController(
            IProcessSubmittedReportService processSubmittedReportService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<AgencyReportController> logger)
        {
            this._processSubmittedReportService = processSubmittedReportService;
            this._notifyService = notifyService;
            this._mailService = mailboxService;
            this._logger = logger;
            this._backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

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
                var (vendor, contract) = await _processSubmittedReportService.SubmitToVendorSupervisor(userEmail, caseId, WebUtility.HtmlDecode(remarks));
                if (vendor == null)
                {
                    _notifyService.Error("OOPs !!!..Error submitting.");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), ControllerName<AgentController>.Name, new { id = caseId });
                }
                _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseReportSubmitToVendorSupervisor(userEmail, caseId, _baseUrl));

                _notifyService.Custom($"Case <b> #{contract}</b> report submitted", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(AgentController.Agent), ControllerName<AgentController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for CaseId {Id}. {UserEmail}.", caseId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AgentController.GetInvestigate), ControllerName<AgentController>.Name, new { id = caseId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, long claimId, string remarks, IFormFile? supervisorAttachment)
        {
            string userEmail = HttpContext?.User?.Identity.Name;
            if (!ModelState.IsValid || claimId < 1)
            {
                _notifyService.Error("Error in submitting report");
                return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
            }
            try
            {
                if (supervisorAttachment != null && supervisorAttachment.Length > 0)
                {
                    if (supervisorAttachment.Length > MAX_FILE_SIZE)
                    {
                        _notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
                    }
                    var ext = Path.GetExtension(supervisorAttachment.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        _notifyService.Error($"Invalid Document image type");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
                    }
                    if (!AllowedMime.Contains(supervisorAttachment.ContentType))
                    {
                        _notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(supervisorAttachment))
                    {
                        _notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
                    }
                }
                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await _processSubmittedReportService.ProcessAgentReport(userEmail, supervisorRemarks, claimId, reportUpdateStatus, supervisorAttachment, remarks);

                if (success != null)
                {
                    _backgroundJobClient.Enqueue(() => _mailService.NotifyCaseReportSubmitToCompany(userEmail, claimId, _baseUrl));

                    _notifyService.Custom($"Case <b> #{success.PolicyDetail.ContractNumber}</b>  Report submitted to Company", 3, "green", "far fa-file-powerpoint");
                }
                else
                {
                    _notifyService.Custom($"Case <b> #{success.PolicyDetail.ContractNumber}</b>  Report sent to review", 3, "orange", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(VendorInvestigationController.CaseReport), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), ControllerName<VendorInvestigationController>.Name, new { selectedcase = claimId });
            }
        }
    }
}