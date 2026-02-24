using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyServiceController : Controller
    {
        private readonly IAgencyServiceTypeManager _agencyServiceTypeManager;
        private readonly IAgencyServiceService _agencyService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<AvailableAgencyServiceController> _logger;
        private readonly INotyfService _notifyService;

        public AvailableAgencyServiceController(
            IAgencyServiceTypeManager agencyServiceTypeManager,
            IAgencyServiceService agencyService,
            INavigationService navigationService,
            ILogger<AvailableAgencyServiceController> logger,
            INotyfService notifyService)
        {
            _agencyServiceTypeManager = agencyServiceTypeManager;
            _agencyService = agencyService;
            _navigationService = navigationService;
            _logger = logger;
            _notifyService = notifyService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Service(long id)
        {
            if (id <= 0)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var model = new ServiceModel { Id = id };
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceManagerPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await _agencyService.PrepareCreateAsync(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceActionPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Add Service", "Create");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting agency service. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service, long VendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await _agencyServiceTypeManager.CreateAsync(service, userEmail);

                if (!result.Success)
                {
                    _notifyService.Custom(result.Message, 3, "red", "fas fa-cog");
                }
                else
                {
                    _notifyService.Custom(result.Message, 3, "green", "fas fa-cog");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service for {AgencyId}. {UserEmail}", VendorId, userEmail);
                _notifyService.Error("Error creating service. Try again.");
            }
            return RedirectToAction(nameof(Service), ControllerName<AvailableAgencyServiceController>.Name, new { id = service.VendorId });
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    _notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                var serviceType = await _agencyService.PrepareEditViewModelAsync(id);

                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceActionPath(serviceType.VendorId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit Service", "Edit");

                return View(serviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service for {ServiceId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting agency service. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorInvestigationServiceType service)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var result = await _agencyServiceTypeManager.EditAsync(service, userEmail);
                if (!result.Success)
                {
                    _notifyService.Custom(result.Message, 3, "red", "fas fa-cog");
                }
                else
                {
                    _notifyService.Custom(result.Message, 3, "orange", "fas fa-cog");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Service. {UserEmail}", userEmail);
                _notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-cog");
            }
            return RedirectToAction(nameof(Service), ControllerName<AvailableAgencyServiceController>.Name, new { id = service.VendorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var serviceDeleted = await _agencyService.DeleteServiceAsync(id);

                if (!serviceDeleted)
                    return NotFound("Service Not Found");

                return Ok(new { success = true, message = "Service deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {ServiceId}", id);
                return StatusCode(500, new { success = false, message = "Delete failed." });
            }
        }
    }
}