using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Assessor
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ICaseVendorService caseVendorService;
        private readonly IInvoiceService invoiceService;
        private readonly IInvestigationDetailService investigationService;
        private readonly ILogger<AssessorController> logger;

        public AssessorController(INotyfService notifyService,
            ICaseVendorService caseVendorService,
            IInvoiceService invoiceService,
            IInvestigationDetailService investigationService,
            ILogger<AssessorController> logger)
        {
            this.notifyService = notifyService;
            this.caseVendorService = caseVendorService;
            this.invoiceService = invoiceService;
            this.investigationService = investigationService;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Assessor));
        }

        [Breadcrumb(" Assess(report)")]
        public IActionResult Assessor()
        {
            return View();
        }

        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        public async Task<IActionResult> GetInvestigateReport(long selectedcase)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var model = await caseVendorService.GetInvestigateReport(userEmail, selectedcase);

                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the case {Id}. {UserEmail}", selectedcase, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        public async Task<IActionResult> SendEnquiry(long selectedcase)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = await caseVendorService.GetInvestigateReport(userEmail, selectedcase);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Assess(report)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("GetInvestigateReport", "Assessor", $"Details") { Parent = agencyPage, RouteValues = new { selectedcase = selectedcase } };
                var editPage = new MvcBreadcrumbNode("SendEnquiry", "Assessor", $"Send Enquiry") { Parent = detailsPage, RouteValues = new { id = selectedcase } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the enquiry case {Id}. {UserEmail}", selectedcase, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Enquiry")]
        public IActionResult Review()
        {
            return View();
        }

        [Breadcrumb(title: " Details", FromAction = "Review")]
        public async Task<IActionResult> ReviewDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await investigationService.GetClaimDetailsReport(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the review case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Approved")]
        public IActionResult Approved()
        {
            return View();
        }

        [Breadcrumb(" Details", FromAction = "Approved")]
        public async Task<IActionResult> ApprovedDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await investigationService.GetClaimDetailsReport(userEmail, id);
                //if (model != null && model.ReportAiSummary == null && model.ClaimsInvestigation.AiEnabled)
                //{
                //    model = await investigationService.GetClaimDetailsAiReportSummary(model);
                //}
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the approved case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Rejected")]
        public IActionResult Rejected()
        {
            return View();
        }

        [Breadcrumb(" Details", FromAction = "Rejected")]
        public async Task<IActionResult> RejectDetail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await investigationService.GetClaimDetailsReport(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the rejected case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Invoice", FromAction = "ApprovedDetail")]
        public async Task<IActionResult> ShowInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var claimsPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Approved", "Assessor", "Approved") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("ApprovedDetail", "Assessor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "Assessor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting the invoice case detail {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("OOPs !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("OOPS !!! Case Not Found !!!..");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            try
            {
                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

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