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

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Claims")]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly IClaimPolicyService claimPolicyService;
        private readonly IInvoiceService invoiceService;
        private readonly IChatSummarizer chatSummarizer;
        private readonly IInvestigationReportService investigationReportService;

        public AssessorController(INotyfService notifyService,
            IClaimPolicyService claimPolicyService,
            IInvoiceService invoiceService,
            IChatSummarizer chatSummarizer,
            IInvestigationReportService investigationReportService)
        {
            this.notifyService = notifyService;
            this.claimPolicyService = claimPolicyService;
            this.invoiceService = invoiceService;
            this.chatSummarizer = chatSummarizer;
            this.investigationReportService = investigationReportService;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Assessor");

        }

        [Breadcrumb(" Assess(report)")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
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
        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase == null)
                {
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = investigationReportService.GetInvestigateReport(currentUserEmail, selectedcase);
                if(model != null && model.ClaimsInvestigation != null && model.ClaimsInvestigation.AiEnabled)
                {
                    var investigationSummary = await chatSummarizer.SummarizeDataAsync(model.ClaimsInvestigation);
                    model.ReportAiSummary = investigationSummary;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public IActionResult SendEnquiry(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase == null)
                {
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = investigationReportService.GetInvestigateReport(currentUserEmail, selectedcase);
                ViewData["claimId"] = selectedcase;


                var claimsPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Claims");
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

        [Breadcrumb(title: "Previous Reports", FromAction = "GetInvestigateReport")]
        public IActionResult PreviousReports(long id)
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
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = investigationReportService.GetPreviousReport(id);

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
        public async Task<IActionResult> ReviewDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await claimPolicyService.GetClaimDetail(id);

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
        public async Task<IActionResult> ApprovedDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.SubmittedDetail(id, currentUserEmail);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: " Rejected")]
        public IActionResult Rejected()
        {
            return View();
        }
        [Breadcrumb(" Details", FromAction = "Rejected")]
        public async Task<IActionResult> RejectDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(id))
                {
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await investigationReportService.SubmittedDetail(id, currentUserEmail);
                if (model != null && model.ClaimsInvestigation != null && model.ClaimsInvestigation.AiEnabled)
                {
                    var investigationSummary = await chatSummarizer.SummarizeDataAsync(model.ClaimsInvestigation);
                    model.ReportAiSummary = investigationSummary;
                }
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
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var invoice = await invoiceService.GetInvoice(id);

                var claimsPage = new MvcBreadcrumbNode("Assessor", "Assessor", "Claims");
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
                    notifyService.Error("OOPS !!! Claim Not Found !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var invoice = await invoiceService.GetInvoice(id);

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
