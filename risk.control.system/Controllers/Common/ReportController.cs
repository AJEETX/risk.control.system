using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Services.Report;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class ReportController : Controller
    {
        private readonly INotyfService _notifyService;
        private readonly ILogger<ReportController> _logger;
        private readonly IInvestigationReportPdfService _reportPdfService;

        public ReportController(INotyfService notifyService,
            ILogger<ReportController> logger,
            IInvestigationReportPdfService reportPdfService
            )
        {
            _notifyService = notifyService;
            _logger = logger;
            _reportPdfService = reportPdfService;
        }

        [HttpGet]
        public async Task<IActionResult> PrintPdfReport(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (id < 1)
                {
                    _notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }

                var claim = await _reportPdfService.GetClaimPdfReport(userEmail, id);

                var fileName = Path.GetFileName(claim.ClaimsInvestigation!.InvestigationReport!.PdfReportFilePath);
                var memory = new MemoryStream();
                using var stream = new FileStream(claim.ClaimsInvestigation.InvestigationReport.PdfReportFilePath!, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                //notifyService.Success($"Policy {claim.PolicyDetail.ContractNumber} Report download success !!!");
                return File(memory, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred to Print Pdf for {CaseId} . {UserEmail}", id, userEmail);
                _notifyService.Error("Error occurred. Try again.");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
        }
    }
}