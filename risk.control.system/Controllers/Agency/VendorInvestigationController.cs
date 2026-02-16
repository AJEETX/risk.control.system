using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class VendorInvestigationController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IInvoiceService invoiceService;
        private readonly INavigationService navigationService;
        private readonly IAgencyInvestigationDetailService vendorInvestigationDetailService;
        private readonly ILogger<VendorInvestigationController> logger;
        private readonly ICaseReportService vendorService;

        public VendorInvestigationController(INotyfService notifyService,
            IInvoiceService invoiceService,
            INavigationService navigationService,
            IAgencyInvestigationDetailService vendorInvestigationDetailService,
            ILogger<VendorInvestigationController> logger,
            ICaseReportService vendorService)
        {
            this.notifyService = notifyService;
            this.invoiceService = invoiceService;
            this.navigationService = navigationService;
            this.vendorInvestigationDetailService = vendorInvestigationDetailService;
            this.logger = logger;
            this.vendorService = vendorService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Allocate));
        }

        [Breadcrumb(" Allocate/Enquiry")]
        public IActionResult Allocate()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb("Agents", FromAction = nameof(Allocate))]
        public async Task<IActionResult> SelectVendorAgent(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { id = id });
                }

                var model = await vendorInvestigationDetailService.SelectVendorAgent(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpGet]
        [Breadcrumb("Re-Allocate", FromAction = nameof(CaseReport))]
        public async Task<IActionResult> ReSelectVendorAgent(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { id = id });
                }

                var model = await vendorInvestigationDetailService.SelectVendorAgent(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Submit(report)")]
        public IActionResult CaseReport()
        {
            return View();
        }

        [Breadcrumb("Submit", FromAction = nameof(CaseReport))]
        public async Task<IActionResult> GetInvestigateReport(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case.");
                    return RedirectToAction(nameof(CaseReport));
                }

                var model = await vendorService.GetInvestigateReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Active")]
        public IActionResult Open()
        {
            return View();
        }

        [Breadcrumb(title: "Details", FromAction = nameof(Allocate))]
        public async Task<IActionResult> CaseDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Allocate));
                }

                var model = await vendorInvestigationDetailService.GetClaimDetails(userEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Details", FromAction = nameof(Open))]
        public async Task<IActionResult> Detail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Open));
                }

                var model = await vendorInvestigationDetailService.GetClaimDetails(userEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Completed")]
        public IActionResult Completed()
        {
            return View();
        }

        [Breadcrumb("Details", FromAction = nameof(Completed))]
        public async Task<IActionResult> CompletedDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Completed));
            }
            try
            {
                var model = await vendorInvestigationDetailService.GetClaimDetailsReport(userEmail, id);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Reply Enquiry", FromAction = nameof(Allocate))]
        public async Task<IActionResult> ReplyEnquiry(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Allocate));
            }
            var model = await vendorService.GetInvestigateReport(userEmail, id);

            return View(model);
        }

        public async Task<IActionResult> ShowInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Completed));
                }
                var invoice = await invoiceService.GetInvoice(id);

                ViewData["BreadcrumbNode"] = navigationService.GetInvoiceBreadcrumb(id, invoice.CaseId.Value, "VendorInvestigation", "VendorInvestigation", "Cases", "Approved", "Approved", "CompletedDetail");

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> PrintInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var invoice = await invoiceService.GetInvoice(id);

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}