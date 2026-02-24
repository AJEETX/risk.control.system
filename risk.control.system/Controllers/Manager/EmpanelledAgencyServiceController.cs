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
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb("Manage Agency")]
    public class EmpanelledAgencyServiceController : Controller
    {
        private readonly INotyfService _notifyService;
        private readonly INavigationService _navigationService;
        private readonly IAgencyServiceTypeManager _agencyServiceTypeManager;
        private readonly ILogger<EmpanelledAgencyServiceController> _logger;
        private readonly IAgencyServiceService _agencyService;

        public EmpanelledAgencyServiceController(
            INavigationService navigationService,
            IAgencyServiceTypeManager agencyServiceTypeManager,
            INotyfService notifyService,
            IAgencyServiceService agencyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyServiceController> logger)
        {
            _navigationService = navigationService;
            _agencyServiceTypeManager = agencyServiceTypeManager;
            _notifyService = notifyService;
            _agencyService = agencyService;
            _logger = logger;
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

            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceManagerPath(id, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            try
            {
                var model = await _agencyService.PrepareCreateAsync(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceActionPath(id, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Add Service", "Create");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred creating Service for {AgencyId} . {UserEmail}", id, HttpContext.User.Identity.Name);
                _notifyService.Error("Error occurred. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInvestigationServiceType service, long VendorId)
        {
            try
            {
                var email = HttpContext.User?.Identity?.Name;

                var result = await _agencyServiceTypeManager.CreateAsync(service, email);

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
                _logger.LogError(ex, "Error occurred creating Service for {AgencyId} . {UserEmail}", VendorId, HttpContext.User.Identity.Name);
                _notifyService.Error("Error creating agency service. Try again.");
            }
            return RedirectToAction(nameof(Service), ControllerName<EmpanelledAgencyServiceController>.Name, new { id = service.VendorId });
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

                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyServiceActionPath(serviceType.VendorId, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Edit Service", "Edit");

                return View(serviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred getting Service for {ServiceId}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error occurred. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorInvestigationServiceType service, long VendorId)
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
                _logger.LogError(ex, "Error occurred editing Service. {UserEmail}", userEmail);
                _notifyService.Error("Error editing agency service. Try again.");
            }
            return RedirectToAction(nameof(Service), ControllerName<EmpanelledAgencyServiceController>.Name, new { id = service.VendorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(long id)
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