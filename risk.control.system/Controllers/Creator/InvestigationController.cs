using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class InvestigationController : Controller
    {
        private readonly ILogger<InvestigationController> logger;
        private readonly ApplicationDbContext context;
        private readonly IAgencyDetailService vendorDetailService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationDetailService investigationDetailService;
        private readonly IEmpanelledAgencyService empanelledAgencyService;

        public InvestigationController(ILogger<InvestigationController> logger,
            ApplicationDbContext context,
            IAgencyDetailService vendorDetailService,
            INotyfService notifyService,
            IInvestigationDetailService investigationDetailService,
            IEmpanelledAgencyService empanelledAgencyService)
        {
            this.logger = logger;
            this.context = context;
            this.vendorDetailService = vendorDetailService;
            this.notifyService = notifyService;
            this.investigationDetailService = investigationDetailService;
            this.empanelledAgencyService = empanelledAgencyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReportTemplate(long caseId)
        {
            if (caseId <= 0)
            {
                return BadRequest("Invalid case id.");
            }

            try
            {
                var template = await empanelledAgencyService.GetReportTemplate(caseId);

                if (template == null)
                {
                    return NotFound();
                }

                return PartialView("_ReportTemplate", template);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting report template. CaseId: {CaseId}, User: {UserEmail}", caseId, User.Identity?.Name ?? "Anonymous");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EmpanelledVendors(long id, long vendorId = 0, bool fromEditPage = false)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    notifyService.Error("No Case selected!!!. Please select Case to allocate.");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                var model = await empanelledAgencyService.GetEmpanelledVendors(id);
                model.FromEditPage = fromEditPage;
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (vendorId > 0)
                {
                    model.VendorId = vendorId;
                }
                BuildEmpanelledBreadcrumb(id, vendorId);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Empanelled Agencies of case {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting Agencies. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
        }

        [Breadcrumb("Details", FromAction = "New", FromController = typeof(CaseCreateEditController))]
        public async Task<IActionResult> Details(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id < 1)
            {
                notifyService.Error("Case Not Found.Try Again");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
            try
            {
                var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
                ViewData["Currency"] = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var model = await investigationDetailService.GetCaseDetails(userEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting case details {Id}. {UserEmail}", id, userEmail);
                notifyService.Error("Error getting case details. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
        }

        public async Task<IActionResult> VendorDetail(long id, long selectedcase)
        {
            if (id <= 0 || selectedcase <= 0)
            {
                notifyService.Error("Invalid request.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }

            var userEmail = User.Identity?.Name;

            try
            {
                var vendor = await vendorDetailService.GetVendorDetailAsync(id, selectedcase);

                if (vendor == null)
                {
                    notifyService.Error("Agency not found.");
                    return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
                }

                BuildBreadcrumb(selectedcase, id);

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error getting agency details. VendorId: {VendorId}, User: {UserEmail}",
                    id,
                    userEmail ?? "Anonymous");

                notifyService.Error("Error getting agency details. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), "CaseCreateEdit");
            }
        }

        private void BuildEmpanelledBreadcrumb(long caseId, long vendorId)
        {
            var casesPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Cases");
            var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign")
            {
                Parent = casesPage
            };

            var detailsPage = new MvcBreadcrumbNode("Details", "Investigation", "Details") { Parent = agencyPage, RouteValues = new { id = caseId } };

            var empanelledPage = new MvcBreadcrumbNode("EmpanelledVendors", "Investigation", "Empanelled Agencies") { Parent = detailsPage, RouteValues = new { id = caseId, vendorId = vendorId } };

            ViewData["BreadcrumbNode"] = empanelledPage;
        }

        private void BuildBreadcrumb(long caseId, long vendorId)
        {
            var claimsPage = new MvcBreadcrumbNode("New", "CaseCreateEdit", "Case");
            var agencyPage = new MvcBreadcrumbNode("Create", "CaseCreateEdit", "Add/Assign")
            {
                Parent = claimsPage
            };
            var detailsPage = new MvcBreadcrumbNode("Details", "Investigation", "Details") { Parent = agencyPage, RouteValues = new { id = caseId } };

            var empanelledPage = new MvcBreadcrumbNode(
                "EmpanelledVendors",
                "Investigation",
                "Empanelled Agencies")
            {
                Parent = detailsPage,
                RouteValues = new { id = caseId }
            };

            var editPage = new MvcBreadcrumbNode(
                "VendorDetail",
                "Investigation",
                "Agency Detail")
            {
                Parent = empanelledPage,
                RouteValues = new { id = vendorId }
            };

            ViewData["BreadcrumbNode"] = editPage;
        }
    }
}