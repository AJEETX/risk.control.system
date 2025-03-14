using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Claims")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class SupervisorController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IInvoiceService invoiceService;
        private readonly IClaimsVendorService vendorService;
        private readonly IInvestigationReportService investigationReportService;

        public SupervisorController(INotyfService notifyService,
            IInvoiceService invoiceService,
            IClaimsVendorService vendorService, 
            IInvestigationReportService investigationReportService)
        {
            this.notifyService = notifyService;
            this.invoiceService = invoiceService;
            this.vendorService = vendorService;
            this.investigationReportService = investigationReportService;
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(" Allocate(new)")]
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Agents", FromAction = "Allocate")]
        public async Task<IActionResult> SelectVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
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
                var model = await vendorService.SelectVendorAgent(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Re-Allocate", FromAction = "ClaimReport")]
        public async Task<IActionResult> ReSelectVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
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
                var model = await vendorService.ReSelectVendorAgent(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Submit(report)")]
        public IActionResult ClaimReport()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }


        [Breadcrumb("Submit", FromAction = "ClaimReport")]

        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case.");
                    return RedirectToAction(nameof(ClaimReport));
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                ViewData["DwellTypeList"] = HtmlHelperExtensions.GetEnumSelectList<DwellType>();
                ViewBag.IncomeList = Enum.GetValues(typeof(Income))
                    .Cast<Income>()
                    .Select(x => new SelectListItem
                    {
                        Value = x.ToString(), // Enum name (e.g., "TAXFREE_SLOT")
                        Text = x.GetType()
                               .GetField(x.ToString())
                               .GetCustomAttribute<DisplayAttribute>()?.Name ?? x.ToString() // Display name or fallback to enum name
                    })
                    .ToList();

                var model = await vendorService.GetInvestigateReport(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
        public async Task<IActionResult> CaseDetail(string id)
        {
            try
            {
                if (id == null)
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
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Open")]
        public async Task<IActionResult> Detail(string id)
        {
            try
            {
                if (id == null)
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
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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

        public async Task<IActionResult> CompletedDetail(string id)
        {
            if (id == null)
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
                var model = await investigationReportService.SubmittedDetail(id, currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Reply Enquiry", FromAction = "Allocate")]

        public async Task<IActionResult> ReplyEnquiry(string id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (string.IsNullOrWhiteSpace(id))
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
                if (id < 1)
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

                var claimsPage = new MvcBreadcrumbNode("Allocate", "SuperVisor", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Completed", "SuperVisor", "Completed") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("CompletedDetail", "SuperVisor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "SuperVisor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            try
            {
                if (id < 1)
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
