﻿using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Agency
{
    [Breadcrumb(" Claims")]
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
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return RedirectToAction("Allocate");
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [Breadcrumb(" Allocate(new)")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public ActionResult Allocate()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
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

        [HttpGet]
        [Breadcrumb("Agents", FromAction = "Allocate")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> SelectVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.SelectVendorAgent(userEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }


        [HttpGet]
        [Breadcrumb("Re-Allocate", FromAction = "ClaimReport")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> ReSelectVendorAgent(string selectedcase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(SelectVendorAgent), new { selectedcase = selectedcase });
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.ReSelectVendorAgent(userEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Submit(report)")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult ClaimReport()
        {
            return View();
        }


        [Breadcrumb("Submit", FromAction = "ClaimReport")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]

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
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = await vendorService.GetInvestigateReport(currentUserEmail, selectedcase);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(" Active")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult Open()
        {
            return View();
        }

        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
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
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Details", FromAction = "Open")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
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
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Completed")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public IActionResult Completed()
        {
            return View();
        }

        [Breadcrumb(" Details", FromAction = "Completed")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]

        public async Task<IActionResult> CompletedDetail(string id)
        {
            if (id == null)
            {
                notifyService.Error("NOT FOUND !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var model = await investigationReportService.SubmittedDetail(id);
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Reply Enquiry", FromAction = "Allocate")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]

        public IActionResult ReplyEnquiry(string id)
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
            var model = investigationReportService.GetInvestigateReport(currentUserEmail, id);
            ViewData["claimId"] = id;

            return View(model);
        }

        [Breadcrumb(title: "Invoice", FromAction = "CompletedDetail")]
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
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
                if (id < 1)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var invoice = await invoiceService.GetInvoice(id);

                var claimsPage = new MvcBreadcrumbNode("Allocate", "SuperVisor", "Claims");
                var agencyPage = new MvcBreadcrumbNode("Completed", "SuperVisor", "Completed") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("CompletedDetail", "SuperVisor", $"Details") { Parent = agencyPage, RouteValues = new { id = invoice.ClaimId } };
                var editPage = new MvcBreadcrumbNode("ShowInvoice", "SuperVisor", $"Invoice") { Parent = detailsPage, RouteValues = new { id = id } };
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
        [Authorize(Roles = "AGENCY_ADMIN,SUPERVISOR")]
        public async Task<IActionResult> PrintInvoice(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail) || 1 > id)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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