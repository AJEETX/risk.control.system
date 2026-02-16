using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Assessor
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ICaseReportService caseVendorService;
        private readonly INavigationService navigationService;
        private readonly IInvoiceService invoiceService;
        private readonly IInvestigationDetailService investigationService;
        private readonly ILogger<AssessorController> logger;

        public AssessorController(INotyfService notifyService,
            ICaseReportService caseVendorService,
            INavigationService navigationService,
            IInvoiceService invoiceService,
            IInvestigationDetailService investigationService,
            ILogger<AssessorController> logger)
        {
            this.notifyService = notifyService;
            this.caseVendorService = caseVendorService;
            this.navigationService = navigationService;
            this.invoiceService = invoiceService;
            this.investigationService = investigationService;
            this.logger = logger;
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
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Assessor));
                }
                var model = await caseVendorService.GetInvestigateReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the case {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
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
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Assessor));
                }
                var model = await caseVendorService.GetInvestigateReport(userEmail, id);

                ViewData["BreadcrumbNode"] = navigationService.GetAssessorEnquiryPath(id, "Assessor"); ;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the enquiry case {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
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
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Review));
                }

                var model = await investigationService.GetClaimDetailsReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the review case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
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
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Approved));
                }

                var model = await investigationService.GetClaimDetailsReport(userEmail, id);
                //if (model != null && model.ReportAiSummary == null && model.ClaimsInvestigation.AiEnabled)
                //{
                //    model = await investigationService.GetClaimDetailsAiReportSummary(model);
                //}
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the approved case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
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
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return RedirectToAction(nameof(Rejected));
            }
            try
            {
                var model = await investigationService.GetClaimDetailsReport(userEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the rejected case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> ShowInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return RedirectToAction(nameof(Approved));
            }
            try
            {
                var invoice = await invoiceService.GetInvoice(id);
                ViewData["BreadcrumbNode"] = navigationService.GetInvoiceBreadcrumb(id, invoice.CaseId.Value, "Assessor", "Assessor", "Cases", "Approved", "Approved", "ApprovedDetail");

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the invoice case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> PrintInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            try
            {
                var invoice = await invoiceService.GetInvoice(id);

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred priting the invoice case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}