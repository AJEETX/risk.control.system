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
    public class AvailableAgencyServiceController(
            IAgencyServiceTypeManager agencyServiceTypeManager,
            IEncryptionService encryptionService,
            IAgencyServiceService agencyService,
            INavigationService navigationService,
            ILogger<AvailableAgencyServiceController> logger,
            INotyfService notifyService) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IAgencyServiceTypeManager _agencyServiceTypeManager = agencyServiceTypeManager;
        private readonly IAgencyServiceService _agencyService = agencyService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly ILogger<AvailableAgencyServiceController> _logger = logger;
        private readonly INotyfService _notifyService = notifyService;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Service([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id <= 0)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
            var model = new ServiceModel { Id = id };
            var encryptedId = _encryptionService.ProtectId(id);
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceManagerPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var model = await _agencyService.PrepareCreateAsync(id);
                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceActionPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Add Service", "Create");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting agency service. Try again.");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
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
                _logger.LogError(ex, "Error creating service. {UserEmail}", userEmail);
                _notifyService.Error("Error creating service. Try again.");
            }
            var encryptedId = _encryptionService.ProtectId(service.VendorId);
            return RedirectToAction(nameof(Service), ControllerName<AvailableAgencyServiceController>.Name, new { id = encryptedId });
        }

        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    _notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
                }
                var serviceType = await _agencyService.PrepareEditViewModelAsync(id);

                var encryptedId = _encryptionService.ProtectId(serviceType.VendorId);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceActionPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit Service", "Edit");

                return View(serviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service for {ServiceId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting agency service. Try again.");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, VendorInvestigationServiceType model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                model.Id = id;
                model.StateId = model.SelectedStateId;
                model.CountryId = model.SelectedCountryId;

                ModelState.Remove(nameof(model.Id));
                ModelState.Remove(nameof(model.StateId));
                ModelState.Remove(nameof(model.CountryId));

                if (!ModelState.IsValid)
                {
                    _notifyService.Custom("Unexpected Error. Please try again later", 3, "orange", "fas fa-cog");
                    return RedirectToAction(nameof(Edit), new { id = id });
                }
                var result = await _agencyServiceTypeManager.EditAsync(model, userEmail);
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
            var encryptedId = _encryptionService.ProtectId(model.VendorId);
            return RedirectToAction(nameof(Service), ControllerName<AvailableAgencyServiceController>.Name, new { id = encryptedId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
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