using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using NToastNotify;

using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Helpers;
//using QuestPDF.Fluent;
namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ICaseVendorService caseVendorService;
        private readonly IInvoiceService invoiceService;
        private readonly IInvestigationService investigationService;
        private readonly IChatSummarizer chatSummarizer;

        public AssessorController(INotyfService notifyService,
            ICaseVendorService caseVendorService,
            IInvoiceService invoiceService,
            IInvestigationService investigationService,
            IChatSummarizer chatSummarizer)
        {
            this.notifyService = notifyService;
            this.caseVendorService = caseVendorService;
            this.invoiceService = invoiceService;
            this.investigationService = investigationService;
            this.chatSummarizer = chatSummarizer;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Assessor");

        }

        [Breadcrumb(" Assess(report)")]
        public IActionResult Assessor()
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        public async Task<IActionResult> GetInvestigateReport(long selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = await caseVendorService.GetInvestigateReport(currentUserEmail, selectedcase);
                if(model != null && model.ClaimsInvestigation != null && model.ClaimsInvestigation.AiEnabled)
                {
                    var investigationSummary = await chatSummarizer.SummarizeDataAsync(model.ClaimsInvestigation);
                    model.InvestigationReport.AiSummaryUpdated = DateTime.Now;
                    model.ReportAiSummary = investigationSummary;
                }
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public async Task<IActionResult> SendEnquiry(long selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = await caseVendorService.GetInvestigateReport(currentUserEmail, selectedcase);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var claimsPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Assess(report)") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("GetInvestigateReport", "Assessor", $"Details") { Parent = agencyPage, RouteValues = new { selectedcase = selectedcase } };
                var editPage = new MvcBreadcrumbNode("SendEnquiry", "Assessor", $"Send Enquiry") { Parent = detailsPage, RouteValues = new { id = selectedcase } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        
        [Breadcrumb(title: "Review")]
        public IActionResult Review()
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: " Details", FromAction = "Review")]
        public async Task<IActionResult> ReviewDetail(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationService.GetClaimDetailsReport(currentUserEmail, id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Approved")]
        public IActionResult Approved()
        {
            return View();
        }
        [Breadcrumb(" Details",FromAction = "Approved")]
        public async Task<IActionResult> ApprovedDetail(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationService.GetClaimDetailsReport(currentUserEmail, id);
                if (model != null && model.ClaimsInvestigation != null && model.ClaimsInvestigation.AiEnabled)
                {
                    var investigationSummary = await chatSummarizer.SummarizeDataAsync(model.ClaimsInvestigation);
                    model.ReportAiSummary = investigationSummary;
                }
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        //public async Task<IActionResult> DownloadReportPdf(long id)
        //{
        //    var currentUserEmail = HttpContext.User?.Identity?.Name;
        //    var report = await investigationService.GetClaimDetailsReport(currentUserEmail, id);

        //    if (report == null)
        //        return NotFound();

        //    var document = new InvestigationReportPdfService(report.ClaimsInvestigation.InvestigationReport);
        //    var pdfBytes = document.GeneratePdf();

        //    return File(pdfBytes, "application/pdf", $"InvestigationReport_{id}.pdf");
        //}
        [Breadcrumb(title: " Rejected")]
        public IActionResult Rejected()
        {
            return View();
        }
        [Breadcrumb(" Details", FromAction = "Rejected")]
        public async Task<IActionResult> RejectDetail(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id < 1)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await investigationService.GetClaimDetailsReport(currentUserEmail, id);
                if (model != null && model.ClaimsInvestigation != null && model.ClaimsInvestigation.AiEnabled)
                {
                    var investigationSummary = await chatSummarizer.SummarizeDataAsync(model.ClaimsInvestigation);
                    model.ReportAiSummary = investigationSummary;
                }
                ViewData["Currency"] = Extensions.GetCultureByCountry(model.ClaimsInvestigation.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }


        [Breadcrumb(title: "Invoice", FromAction = "ApprovedDetail")]
        public async Task<IActionResult> ShowInvoice(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id==0)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var claimsPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Cases");
                var agencyPage = new MvcBreadcrumbNode("Approved", "Assessor", "Approved") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("ApprovedDetail", "Assessor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "Assessor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(invoice);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Print", FromAction = "ShowInvoice")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0)
                {
                    notifyService.Error("OOPS !!! Case Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var invoice = await invoiceService.GetInvoice(id);
                ViewData["Currency"] = Extensions.GetCultureByCountry(invoice.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                return View(invoice);
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
