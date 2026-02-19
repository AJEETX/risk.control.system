using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.AgencyAdmin;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyServiceController : Controller
    {
        private readonly IAgencyServiceService _service;
        private readonly IAgencyServiceTypeManager _typeManager;
        private readonly INotyfService _notifyService;
        private readonly ILogger<AgencyServiceController> _logger;

        public AgencyServiceController(
            IAgencyServiceService service,
            IAgencyServiceTypeManager typeManager,
            INotyfService notifyService,
            ILogger<AgencyServiceController> logger)
        {
            _service = service;
            _typeManager = typeManager;
            _notifyService = notifyService;
            _logger = logger;
        }

        public IActionResult Index() => RedirectToAction(nameof(Service));

        [Breadcrumb("Manage Service")]
        public IActionResult Service() => View();

        [Breadcrumb("Add Service")]
        public async Task<IActionResult> Create()
        {
            var userEmail = User.Identity?.Name;
            try
            {
                var model = await _service.PrepareCreateViewModelAsync(userEmail);
                if (model == null)
                {
                    _notifyService.Error("Agency or User Not Found!");
                    return RedirectToAction(nameof(Service));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create view for {UserEmail}", userEmail);
                _notifyService.Error("Error creating service. Try again.");
                return RedirectToAction(nameof(Service));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType serviceModel)
        {
            try
            {
                var result = await _typeManager.CreateAsync(serviceModel, User.Identity?.Name);
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
                _logger.LogError(ex, "Error creating service");
                _notifyService.Error("Critical error during creation.");
            }
            return RedirectToAction(nameof(Service));
        }

        [Breadcrumb("Edit Service", FromAction = nameof(Service))]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("Index", "Dashboard");

                var model = await _service.PrepareEditViewModelAsync(id);
                if (model == null)
                {
                    _notifyService.Custom("Service not found.", 3, "red", "fas fa-cog");
                    return RedirectToAction(nameof(Service));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit view for ID {Id}", id);
                return RedirectToAction(nameof(Service));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorInvestigationServiceType serviceModel)
        {
            try
            {
                var result = await _typeManager.EditAsync(serviceModel, User.Identity?.Name);
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
                _logger.LogError(ex, "Error updating service");
                _notifyService.Error("Error editing service.");
            }
            return RedirectToAction(nameof(Service));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var success = await _service.DeleteServiceAsync(id);
                if (!success) return NotFound();

                return Ok(new { success = true, message = "Service deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {Id}", id);
                return StatusCode(500, new { success = false, message = "Delete failed." });
            }
        }
    }
}