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
        private readonly IInvestigationReportService investigationReportService;

        public AssessorController(INotyfService notifyService,
            IClaimPolicyService claimPolicyService,
            IInvoiceService invoiceService,
            IInvestigationReportService investigationReportService)
        {
            this.notifyService = notifyService;
            this.claimPolicyService = claimPolicyService;
            this.invoiceService = invoiceService;
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
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(title: "Report", FromAction = "Assessor")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult GetInvestigateReport(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }
                var model = investigationReportService.GetInvestigateReport(currentUserEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult SendEnquiry(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (selectedcase == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
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
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: "Previous Reports", FromAction = "GetInvestigateReport")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult PreviousReports(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = investigationReportService.GetPreviousReport(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: "Review")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Review()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(title: " Details", FromAction = "Review")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ReviewDetail(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await claimPolicyService.GetClaimDetail(id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Approved")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public IActionResult Approved()
        {
            return View();
        }
        [Breadcrumb(" Details",FromAction = "Approved")]
        public async Task<IActionResult> ApprovedDetail(string id)
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
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await investigationReportService.SubmittedDetail(id);

                return View(model);
            }
            catch (Exception)
            {
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
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }


                var model = await investigationReportService.SubmittedDetail(id);

                return View(model);
            }
            catch (Exception)
            {
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
                    notifyService.Error("OOPs !!!..Contact Admin");
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
            catch (Exception)
            {
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
               
                var invoice = await invoiceService.GetInvoice(id);

                return View(invoice);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
