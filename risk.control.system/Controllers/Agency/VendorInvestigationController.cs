using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class VendorInvestigationController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IInvoiceService invoiceService;
        private readonly IVendorInvestigationDetailService vendorInvestigationDetailService;
        private readonly ILogger<VendorInvestigationController> logger;
        private readonly ICaseVendorService vendorService;

        public VendorInvestigationController(INotyfService notifyService,
            IInvoiceService invoiceService,
            IVendorInvestigationDetailService vendorInvestigationDetailService,
            ILogger<VendorInvestigationController> logger,
            ICaseVendorService vendorService)
        {
            this.notifyService = notifyService;
            this.invoiceService = invoiceService;
            this.vendorInvestigationDetailService = vendorInvestigationDetailService;
            this.logger = logger;
            this.vendorService = vendorService;
        }
        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return RedirectToAction("Allocate");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UserName}.", HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(" Allocate/Enquiry")]
        public ActionResult Allocate()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UserName}.", HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Agents", FromAction = "Allocate")]
        public async Task<IActionResult> SelectVendorAgent(long selectedcase)
        {
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorInvestigationDetailService.SelectVendorAgent(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserName}.", selectedcase, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Re-Allocate", FromAction = "CaseReport")]
        public async Task<IActionResult> ReSelectVendorAgent(long selectedcase)
        {
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorInvestigationDetailService.SelectVendorAgent(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserName}.", selectedcase, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Submit(report)")]
        public IActionResult CaseReport()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb("Submit", FromAction = "CaseReport")]
        public async Task<IActionResult> GetInvestigateReport(long selectedcase)
        {
            try
            {
                if (!ModelState.IsValid || selectedcase < 1)
                {
                    notifyService.Error("No case selected!!!. Please select case.");
                    return RedirectToAction(nameof(CaseReport));
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await vendorService.GetInvestigateReport(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {SelectedCase} for {UserName}.", selectedcase, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(" Active")]
        public IActionResult Open()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(title: " Details", FromAction = "Allocate")]
        public async Task<IActionResult> CaseDetail(long id)
        {
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorInvestigationDetailService.GetClaimDetails(currentUserEmail, id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserName}.", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Open")]
        public async Task<IActionResult> Detail(long id)
        {
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorInvestigationDetailService.GetClaimDetails(currentUserEmail, id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserName}.", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Completed")]
        public IActionResult Completed()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb(" Details", FromAction = "Completed")]

        public async Task<IActionResult> CompletedDetail(long id)
        {
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorInvestigationDetailService.GetClaimDetailsReport(currentUserEmail, id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserName}.", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Reply Enquiry", FromAction = "Allocate")]

        public async Task<IActionResult> ReplyEnquiry(long id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var model = await vendorService.GetInvestigateReport(currentUserEmail, id);
            ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            ViewData["claimId"] = id;

            return View(model);
        }

        [Breadcrumb(title: "Invoice", FromAction = "CompletedDetail")]
        public async Task<IActionResult> ShowInvoice(long id)
        {
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("Allocate", "SuperVisor", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Completed", "SuperVisor", "Completed") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("CompletedDetail", "SuperVisor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "SuperVisor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserName}.", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail) || 1 > id)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {CaseId} for {UserName}.", id, HttpContext.User?.Identity?.Name ?? "Anonymous");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
