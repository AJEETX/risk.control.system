using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class ReportController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IInvestigationService investigationService;
        private readonly ICaseService claimsService;

        public ReportController(INotyfService notifyService,
            IInvestigationService investigationService,
            ICaseService claimsService)
        {
            this.notifyService = notifyService;
            this.investigationService = investigationService;
            this.claimsService = claimsService;
        }


        [HttpGet]
        public async Task<IActionResult> PrintPdfReport(long id)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}