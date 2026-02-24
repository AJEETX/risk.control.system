using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Assessor;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Assessor
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController : Controller
    {
        private readonly INotyfService _notifyService;
        private readonly INavigationService _navigationService;
        private readonly IInvoiceService _invoiceService;
        private readonly ICaseDetailReportService _caseDetailReportService;
        private readonly ILogger<AssessorController> _logger;

        public AssessorController(INotyfService notifyService,
            INavigationService navigationService,
            IInvoiceService invoiceService,
            ICaseDetailReportService caseDetailReportService,
            ILogger<AssessorController> logger)
        {
            _notifyService = notifyService;
            _navigationService = navigationService;
            _invoiceService = invoiceService;
            _caseDetailReportService = caseDetailReportService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Assessor));
        }

        [Breadcrumb("Assess(report)")]
        public IActionResult Assessor()
        {
            return View();
        }

        [Breadcrumb(title: "Report", FromAction = nameof(Assessor))]
        public async Task<IActionResult> GetInvestigateReport(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Assessor));
                }
                var model = await _caseDetailReportService.GetInvestigateReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting the case {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> SendEnquiry(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Assessor));
                }
                var model = await _caseDetailReportService.GetInvestigateReport(userEmail, id);

                ViewData["BreadcrumbNode"] = _navigationService.GetAssessorEnquiryPath(id, "Assessor"); ;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting the enquiry case {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Enquiry")]
        public IActionResult Review()
        {
            return View();
        }

        [Breadcrumb(title: "Details", FromAction = nameof(Review))]
        public async Task<IActionResult> ReviewDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Review));
                }

                var model = await _caseDetailReportService.GetClaimDetailsReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting the review case detail {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: nameof(Approved))]
        public IActionResult Approved()
        {
            return View();
        }

        [Breadcrumb("Details", FromAction = nameof(Approved))]
        public async Task<IActionResult> ApprovedDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Approved));
                }

                var model = await _caseDetailReportService.GetClaimDetailsReport(userEmail, id);
                //if (model != null && model.ReportAiSummary == null && model.ClaimsInvestigation.AiEnabled)
                //{
                //    model = await investigationService.GetClaimDetailsAiReportSummary(model);
                //}
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting the approved case detail {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: nameof(Rejected))]
        public IActionResult Rejected()
        {
            return View();
        }

        [Breadcrumb("Details", FromAction = nameof(Rejected))]
        public async Task<IActionResult> RejectDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id < 1)
            {
                _notifyService.Error("OOPS !!! Case Not Found !!!..");
                return RedirectToAction(nameof(Rejected));
            }
            try
            {
                var model = await _caseDetailReportService.GetClaimDetailsReport(userEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting the rejected case detail {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> ShowInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id <= 0)
            {
                _notifyService.Error("OOPS !!! Case Not Found !!!..");
                return RedirectToAction(nameof(Approved));
            }
            try
            {
                var invoice = await _invoiceService.GetInvoice(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetInvoiceBreadcrumb(id, invoice.CaseId.Value, "Assessor", "Assessor", "Cases", "Approved", "Approved", "ApprovedDetail");

                return View(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting the invoice case detail {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> PrintInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id <= 0)
            {
                _notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            try
            {
                var invoice = await _invoiceService.GetInvoice(id);

                return View(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred priting the invoice case detail {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}