using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = MANAGER.DISPLAY_NAME)]
    public class ManagerController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IInvoiceService invoiceService;
        private readonly ILogger<ManagerController> logger;
        private readonly IInvestigationDetailService investigativeService;

        public ManagerController(INotyfService notifyService,
            IInvoiceService invoiceService,
            ILogger<ManagerController> logger,
            IInvestigationDetailService investigativeService)
        {
            this.notifyService = notifyService;
            this.invoiceService = invoiceService;
            this.logger = logger;
            this.investigativeService = investigativeService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Assessor");
        }

        [Breadcrumb(" Assess(new)", FromAction = "Index")]
        public IActionResult Assessor()
        {
            return View();
        }

        [Breadcrumb(title: " Details", FromAction = "Assessor")]
        public async Task<IActionResult> AssessorDetail(long id)
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("Oops! Unauthenticated access.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("Case not found.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            try
            {
                var model = await investigativeService.GetClaimDetailsReport(userEmail, id);

                var countryCode = model?.ClaimsInvestigation?
                    .ClientCompany?
                    .Country?
                    .Code?
                    .ToUpperInvariant();

                if (!string.IsNullOrWhiteSpace(countryCode))
                {
                    ViewData["Currency"] = CustomExtensions
                        .GetCultureByCountry(countryCode)
                        .NumberFormat
                        .CurrencySymbol;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error getting case detail for user {UserEmail}, CaseId {CaseId}",
                    userEmail,
                    id);

                notifyService.Error("Error getting case detail. Please try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: "Active")]
        public IActionResult Active()
        {
            return View();
        }

        [Breadcrumb(title: " Details", FromAction = "Active")]
        public async Task<IActionResult> ActiveDetail(long id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                notifyService.Error("Case not found.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("Oops! Unauthenticated access.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            try
            {
                var model = await investigativeService.GetClaimDetailsReport(userEmail, id);

                var countryCode = model?.ClaimsInvestigation?
                    .ClientCompany?
                    .Country?
                    .Code?
                    .ToUpperInvariant();

                if (!string.IsNullOrWhiteSpace(countryCode))
                {
                    ViewData["Currency"] = CustomExtensions
                        .GetCultureByCountry(countryCode)
                        .NumberFormat
                        .CurrencySymbol;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error getting active case detail. User: {UserEmail}, CaseId: {CaseId}",
                    userEmail,
                    id);

                notifyService.Error("Error getting case detail. Please try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [Breadcrumb(title: " Approved")]
        public IActionResult Approved()
        {
            return View();
        }

        [Breadcrumb(title: " Details", FromAction = "Approved")]
        public async Task<IActionResult> ApprovedDetail(long id)
        {
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("Case Not Found !!!..");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await investigativeService.GetClaimDetailsReport(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting approved case detail.User: {UserEmail}, CaseId: {CaseId}",
                    userEmail,
                    id);
                notifyService.Error("Error getting case detail. Try again.");
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
                notifyService.Error("Case Not Found !!!..");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await investigativeService.GetClaimDetailsReport(userEmail, id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting rejected case detail.User: {UserEmail}, CaseId: {CaseId}",
                    userEmail,
                    id);
                notifyService.Error("Error getting case detail. Try again.");
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
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("Case Not Found !!!..");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("Assessor", "Manager", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Approved", "Manager", "Approved") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("ApprovedDetail", "Manager", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "Manager", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting invoice case detail.User: {UserEmail}, CaseId: {CaseId}",
                   userEmail,
                   id);
                notifyService.Error("Error getting invoice detail. Try again.");
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
                notifyService.Error("Case Not Found !!!..");
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
                logger.LogError(ex, "Error getting printing invoice case detail.User: {UserEmail}, CaseId: {CaseId}",
                   userEmail,
                   id);
                notifyService.Error("Error printing invoice. Try again.");
                                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }
    }
}