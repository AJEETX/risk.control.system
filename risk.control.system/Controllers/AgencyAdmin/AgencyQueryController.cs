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
            var userEmail = User.Identity?.Name ?? "Anonymous";
            var redirectOnSuccess = RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            if (!ModelState.IsValid)
            {
                _notifyService.Error("NOT FOUND !!!..");
                return redirectOnSuccess;
            }
            try
            {
                if (document?.Length > 0)
                {
                    var (isValid, errorMessage) = ValidateDocument(document);
                    if (!isValid)
                    {
                        _notifyService.Error(errorMessage!);
                        return RedirectToAction(nameof(VendorInvestigationController.ReplyEnquiry), ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                }
                var enquiry = request.InvestigationReport!.EnquiryRequest!;
                enquiry.DescriptiveAnswer = HttpUtility.HtmlEncode(enquiry.DescriptiveAnswer);
                var replySubmitted = await _agencyQueryReplyService.SubmitQueryReplyToCompany(userEmail, claimId, enquiry, request.InvestigationReport.EnquiryRequests, document);
                if (replySubmitted)
                {
                    _backgroundJobClient.Enqueue(() => _mailService.NotifySubmitReplyToCompany(userEmail, claimId, _baseUrl));
                    _notifyService.Success("Enquiry Reply Sent to Company");
                    return redirectOnSuccess;
                }
                _notifyService.Error("OOPs !!!..Contact Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail);
                _notifyService.Error("An internal error occurred.");
            }

            return redirectOnSuccess;
        }

        private (bool IsValid, string? Error) ValidateDocument(IFormFile document)
        {
            if (document.Length > MAX_FILE_SIZE)
                return (false, "Document image Size exceeds the max size: 5MB");

            var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                return (false, "Invalid Document image type");

            if (!AllowedMime.Contains(document.ContentType))
                return (false, "Invalid Document Image content type");

            if (!ImageSignatureValidator.HasValidSignature(document))
                return (false, "Invalid or corrupted Document Image");

            return (true, null);
        }
    }
}