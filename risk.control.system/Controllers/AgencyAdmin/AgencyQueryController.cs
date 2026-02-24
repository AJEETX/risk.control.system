using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyQueryController : Controller
    {
        private readonly string _baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly IAgencyQueryReplyService _agencyQueryReplyService;
        private readonly INotyfService _notifyService;
        private readonly IMailService _mailService;
        private readonly ILogger<AgencyQueryController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AgencyQueryController(
            IAgencyQueryReplyService agencyQueryReplyService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailService,
            ILogger<AgencyQueryController> logger)
        {
            _agencyQueryReplyService = agencyQueryReplyService;
            _notifyService = notifyService;
            _mailService = mailService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyQuery(long claimId, CaseAgencyModel request, IFormFile? document)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            try
            {
                if (document != null && document.Length > 0)
                {
                    if (document.Length > MAX_FILE_SIZE)
                    {
                        _notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                    var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        _notifyService.Error($"Invalid Document image type");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                    if (!AllowedMime.Contains(document.ContentType))
                    {
                        _notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(document))
                    {
                        _notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                }

                request.InvestigationReport.EnquiryRequest.DescriptiveAnswer = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveAnswer);

                var claim = await _agencyQueryReplyService.SubmitQueryReplyToCompany(userEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, document);

                if (claim != null)
                {
                    _backgroundJobClient.Enqueue(() => _mailService.NotifySubmitReplyToCompany(userEmail, claimId, _baseUrl));

                    _notifyService.Success("Enquiry Reply Sent to Company");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
                }
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                _notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }
    }
}