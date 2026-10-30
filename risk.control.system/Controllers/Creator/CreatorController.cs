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
    public class CreatorController(ILogger<CreatorController> logger,
            IAgencyDetailService agencyDetailService,
            IEncryptionService encryptionService,
            IReportTemplateService reportTemplateService,
            INotyfService notifyService,
            INavigationService navigationService,
            ICaseActiveService caseActiveService,
            IEmpanelledAgencyService empanelledAgencyService) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly ILogger<CreatorController> _logger = logger;
        private readonly IAgencyDetailService _agencyDetailService = agencyDetailService;
        private readonly IReportTemplateService _reportTemplateService = reportTemplateService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly ICaseActiveService _caseActiveService = caseActiveService;
        private readonly IEmpanelledAgencyService _empanelledAgencyService = empanelledAgencyService;

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
        public async Task<IActionResult> EmpanelledAgencies([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, [ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long vendorId = 0, bool fromEditPage = false)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid || id < 1)
                {
                    _notifyService.Error("No Case selected!!!. Please select Case to allocate.");
                    return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
                }

                var model = await _empanelledAgencyService.GetEmpanelledVendors(id, userEmail, vendorId, fromEditPage);

                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetEmpanelledVendorsPath(encryptedId, vendorId, fromEditPage);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Empanelled Agencies of case {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting Agencies. Try again.");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromAction = "New", FromController = typeof(AddAssignController))]
        public async Task<IActionResult> Details([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            if (!ModelState.IsValid || id < 1)
            {
                _notifyService.Error("Case Not Found.Try Again");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
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
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
        }

        public async Task<IActionResult> AgencyProfile([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, long selectedcase)
        {
            if (id <= 0 || selectedcase <= 0)
            {
                _notifyService.Error("Invalid request.");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }

            var userEmail = User.Identity?.Name;

            try
            {
                var vendor = await _agencyDetailService.GetVendorDetailAsync(id, selectedcase);
                if (vendor == null)
                {
                    return NotFound("Agency not found.");
                }

                // If requested via AJAX, return a partial view so it fits neatly inside the modal box
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_AgencyProfileModalPartial", vendor);
                }

                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency details. VendorId: {VendorId}, User: {UserEmail}", id, userEmail ?? "Anonymous");
                _notifyService.Error("Error getting agency details. Try again.");
                return RedirectToAction(nameof(AddAssignController.New), ControllerName<AddAssignController>.Name);
            }
        }
    }
}