using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Report;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class VendorInvestigationController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IInvoiceService invoiceService;
        private readonly IAgencyInvestigationDetailService vendorInvestigationDetailService;
        private readonly ILogger<VendorInvestigationController> logger;
        private readonly ICaseReportService vendorService;

        public VendorInvestigationController(INotyfService notifyService,
            IInvoiceService invoiceService,
            IAgencyInvestigationDetailService vendorInvestigationDetailService,
            ILogger<VendorInvestigationController> logger,
            ICaseReportService vendorService)
        {
            this.notifyService = notifyService;
            this.invoiceService = invoiceService;
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
        [Breadcrumb("Agents", FromAction = "Allocate")]
        public async Task<IActionResult> SelectVendorAgent(long selectedcase)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }

                var model = await vendorInvestigationDetailService.SelectVendorAgent(userEmail, selectedcase);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserEmail}.", selectedcase, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpGet]
        [Breadcrumb("Re-Allocate", FromAction = "CaseReport")]
        public async Task<IActionResult> ReSelectVendorAgent(long selectedcase)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }

                var model = await vendorInvestigationDetailService.SelectVendorAgent(userEmail, selectedcase);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserEmail}.", selectedcase, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb("Submit(report)")]
        public IActionResult CaseReport()
        {
            return View();
        }

        [Breadcrumb("Submit", FromAction = "CaseReport")]
        public async Task<IActionResult> GetInvestigateReport(long selectedcase)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case.");
                    return RedirectToAction(nameof(CaseReport));
                }

                var model = await vendorService.GetInvestigateReport(userEmail, selectedcase);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserEmail}.", selectedcase, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(" Active")]
        public IActionResult Open()
        {
            return View();
        }

        [Breadcrumb(title: " Details", FromAction = "Allocate")]
        public async Task<IActionResult> CaseDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await vendorInvestigationDetailService.GetClaimDetails(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Open")]
        public async Task<IActionResult> Detail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await vendorInvestigationDetailService.GetClaimDetails(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
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

        [Breadcrumb(" Details", FromAction = "Completed")]
        public async Task<IActionResult> CompletedDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("NOT FOUND !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await vendorInvestigationDetailService.GetClaimDetailsReport(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(" Reply Enquiry", FromAction = "Allocate")]
        public async Task<IActionResult> ReplyEnquiry(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("NOT FOUND !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var model = await vendorService.GetInvestigateReport(userEmail, id);
            ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            ViewData["claimId"] = id;

            return View(model);
        }

        [Breadcrumb(title: "Invoice", FromAction = "CompletedDetail")]
        public async Task<IActionResult> ShowInvoice(long id)
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
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("Allocate", "SuperVisor", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Completed", "SuperVisor", "Completed") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("CompletedDetail", "SuperVisor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "SuperVisor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserEmail}.", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
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
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

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