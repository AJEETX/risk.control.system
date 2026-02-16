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
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyQueryController : Controller
    {
        private readonly string baseUrl;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly IAgencyQueryReplyService agencyQueryReplyService;
        private readonly INotyfService notifyService;
        private readonly IMailService mailboxService;
        private readonly ILogger<AgencyQueryController> logger;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient backgroundJobClient;

        public AgencyQueryController(
            IAgencyQueryReplyService agencyQueryReplyService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<AgencyQueryController> logger,
            ApplicationDbContext context)
        {
            this.agencyQueryReplyService = agencyQueryReplyService;
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
        public async Task<IActionResult> ReplyQuery(long claimId, CaseInvestigationVendorsModel request, IFormFile? document)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            try
            {
                if (document != null && document.Length > 0)
                {
                    if (document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                    var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                    if (!AllowedMime.Contains(document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                    if (!ImageSignatureValidator.HasValidSignature(document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        return RedirectToAction("ReplyEnquiry", ControllerName<VendorInvestigationController>.Name, new { id = claimId });
                    }
                }

                request.InvestigationReport.EnquiryRequest.DescriptiveAnswer = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveAnswer);

                var claim = await agencyQueryReplyService.SubmitQueryReplyToCompany(userEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, document);

                if (claim != null)
                {
                    var agencyUser = await _context.ApplicationUser.AsNoTracking().Include(a => a.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitReplyToCompany(userEmail, claimId, baseUrl));

                    notifyService.Success("Enquiry Reply Sent to Company");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for Case {Id}. {UserEmail}.", claimId, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), ControllerName<VendorInvestigationController>.Name);
            }
        }
    }
}