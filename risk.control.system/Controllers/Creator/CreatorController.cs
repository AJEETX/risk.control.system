using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Services;
using risk.control.system.Services.Common;
using risk.control.system.Services.Creator;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Creator
{
    [Breadcrumb("Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CreatorController : Controller
    {
        private readonly ILogger<CreatorController> _logger;
        private readonly IAgencyDetailService _agencyDetailService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly INotyfService _notifyService;
        private readonly INavigationService _navigationService;
        private readonly ICaseActiveService _caseActiveService;
        private readonly IEmpanelledAgencyService _empanelledAgencyService;

        public CreatorController(ILogger<CreatorController> logger,
            IAgencyDetailService agencyDetailService,
            IReportTemplateService reportTemplateService,
            INotyfService notifyService,
            INavigationService navigationService,
            ICaseActiveService caseActiveService,
            IEmpanelledAgencyService empanelledAgencyService)
        {
            _logger = logger;
            _agencyDetailService = agencyDetailService;
            _reportTemplateService = reportTemplateService;
            _notifyService = notifyService;
            _navigationService = navigationService;
            _caseActiveService = caseActiveService;
            _empanelledAgencyService = empanelledAgencyService;
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
                var template = await _reportTemplateService.GetReportTemplate(caseId);

                if (template == null)
                {
                    return NotFound();
                }

                return PartialView("_ReportTemplate", template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report template. CaseId: {CaseId}, User: {UserEmail}", caseId, User.Identity?.Name ?? "Anonymous");

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
                    _notifyService.Error("No Case selected!!!. Please select Case to allocate.");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                var model = await _empanelledAgencyService.GetEmpanelledVendors(id, userEmail, vendorId, fromEditPage);

                ViewData["BreadcrumbNode"] = _navigationService.GetEmpanelledVendorsPath(id, vendorId, fromEditPage);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Empanelled Agencies of case {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting Agencies. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
        }

        [Breadcrumb("Details", FromAction = "New", FromController = typeof(CaseCreateEditController))]
        public async Task<IActionResult> Details(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid || id < 1)
            {
                _notifyService.Error("Case Not Found.Try Again");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
            try
            {
                var model = await _caseActiveService.GetActiveCaseDetails(userEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case details {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting case details. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
        }

        public async Task<IActionResult> VendorDetail(long id, long selectedcase)
        {
            if (id <= 0 || selectedcase <= 0)
            {
                _notifyService.Error("Invalid request.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }

            var userEmail = User.Identity?.Name;

            try
            {
                var vendor = await _agencyDetailService.GetVendorDetailAsync(id, selectedcase);

                if (vendor == null)
                {
                    _notifyService.Error("Agency not found.");
                    return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
                }

                ViewData["BreadcrumbNode"] = _navigationService.GetVendorDetailPath(selectedcase, id);

                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency details. VendorId: {VendorId}, User: {UserEmail}", id, userEmail ?? "Anonymous");
                _notifyService.Error("Error getting agency details. Try again.");
                return RedirectToAction(nameof(CaseCreateEditController.New), ControllerName<CaseCreateEditController>.Name);
            }
        }
    }
}