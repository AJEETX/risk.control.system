using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class ReportController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ILogger<ReportController> logger;
        private readonly IInvestigationDetailService investigationService;

        public ReportController(INotyfService notifyService,
            ILogger<ReportController> logger,
            IInvestigationDetailService investigationService
            )
        {
            this.notifyService = notifyService;
            this.logger = logger;
            this.investigationService = investigationService;
        }

        [HttpGet]
        public async Task<IActionResult> PrintPdfReport(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var claim = await investigationService.GetClaimPdfReport(currentUserEmail, id);

                var fileName = Path.GetFileName(claim.ClaimsInvestigation.InvestigationReport.PdfReportFilePath);
                var memory = new MemoryStream();
                using var stream = new FileStream(claim.ClaimsInvestigation.InvestigationReport.PdfReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                //notifyService.Success($"Policy {claim.PolicyDetail.ContractNumber} Report download success !!!");
                return File(memory, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred to Print Pdf for {CaseId} . {UserEmail}", id, HttpContext.User.Identity.Name);
                notifyService.Error("Error occurred. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}