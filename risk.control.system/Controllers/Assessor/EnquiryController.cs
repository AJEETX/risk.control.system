using System.Web;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
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
        private readonly ApplicationDbContext context;
        private readonly ILogger<EnquiryController> logger;
        private readonly INotyfService notifyService;
        private readonly IAssessorQueryService assessorQueryService;
        private readonly string baseUrl = "";
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IMailService mailService;

        public EnquiryController(
            ApplicationDbContext context,
            ILogger<EnquiryController> logger,
            INotyfService notifyService,
            IAssessorQueryService assessorQueryService,
            IBackgroundJobClient backgroundJobClient,
            IMailService mailService)
        {
            this.context = context;
            this.logger = logger;
            this.notifyService = notifyService;
            this.assessorQueryService = assessorQueryService;
            this.backgroundJobClient = backgroundJobClient;
            this.mailService = mailService;
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
                    notifyService.Error("Bad Request..");
                    return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                }
                if (document != null && document.Length > 0)
                {
                    if (document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                    var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                    if (!AllowedMime.Contains(document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction(nameof(AssessorController.SendEnquiry), ControllerName<AssessorController>.Name, new { id = claimId });
                    }
                }

                request.InvestigationReport.EnquiryRequest.DescriptiveQuestion = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveQuestion);

                var model = await assessorQueryService.SubmitQueryToAgency(userEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, document);
                if (model != null)
                {
                    backgroundJobClient.Enqueue(() => mailService.NotifySubmitQueryToAgency(userEmail, claimId, baseUrl));

                    notifyService.Success("Enquiry Sent to Agency");
                    return RedirectToAction(nameof(AssessorController.Assessor), ControllerName<AssessorController>.Name);
                }
                notifyService.Error("OOPs !!!..Error sending query");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error submitting query case {Id}", claimId, userEmail);
                notifyService.Error("Error submitting query. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}