using System.Web;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Assessor;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Assessor
{
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class EnquiryController : Controller
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly ILogger<EnquiryController> _logger;
        private readonly INotyfService _notifyService;
        private readonly IAssessorQueryService _assessorQueryService;
        private readonly string _baseUrl;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IMailService _mailService;

        public EnquiryController(
            ILogger<EnquiryController> logger,
            INotyfService notifyService,
            IAssessorQueryService assessorQueryService,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundJobClient backgroundJobClient,
            IMailService mailService)
        {
            _logger = logger;
            _notifyService = notifyService;
            _assessorQueryService = assessorQueryService;
            _backgroundJobClient = backgroundJobClient;
            _mailService = mailService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEnquiry([FromForm] CaseAgencyModel request, [FromForm] long claimId, [FromForm] IFormFile? document)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _notifyService.Error("Bad Request..");
                    return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                }
                if (document != null && document.Length > 0)
                {
                    if (document.Length > MAX_FILE_SIZE)
                    {
                        _notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                    var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        _notifyService.Error($"Invalid Document image type");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                    if (!AllowedMime.Contains(document.ContentType))
                    {
                        _notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(document))
                    {
                        _notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                }

                request.InvestigationReport.EnquiryRequest.DescriptiveQuestion = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveQuestion);

                var model = await _assessorQueryService.SubmitQueryToAgency(userEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, document);
                if (model != null)
                {
                    _backgroundJobClient.Enqueue(() => _mailService.NotifySubmitQueryToAgency(userEmail, claimId, _baseUrl));

                    _notifyService.Success("Enquiry Sent to Agency");
                    return RedirectToAction(nameof(AssessorController.Assessor), ControllerName<AssessorController>.Name);
                }
                _notifyService.Error("OOPs !!!..Error sending query");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting query case {Id}", claimId, userEmail);
                _notifyService.Error("Error submitting query. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}