using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;
using risk.control.system.Services.Manager;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyController : Controller
    {
        private readonly IManageAgencyService _manageAgencyService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly INavigationService _navigationService;
        private readonly INotyfService _notifyService;
        private readonly ICompanyAgencyService _companyAgencyService;
        private readonly ILogger<AvailableAgencyController> _logger;
        private readonly string _baseUrl;

        public AvailableAgencyController(
            IManageAgencyService manageAgencyService,
            IErrorNotifyService errorNotifyService,
            INavigationService navigationService,
            INotyfService notifyService,
            ICompanyAgencyService companyAgencyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<AvailableAgencyController> logger)
        {
            _manageAgencyService = manageAgencyService;
            this._errorNotifyService = errorNotifyService;
            _navigationService = navigationService;
            _notifyService = notifyService;
            _companyAgencyService = companyAgencyService;
            _logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Agencies));
        }

        [Breadcrumb("Available Agencies")]
        public IActionResult Agencies()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agencies(List<long> vendors)
        {
            var userEmail = User.Identity?.Name;

            // Basic validation stays in controller
            if (!ModelState.IsValid || vendors == null || vendors.Count == 0)
            {
                _notifyService.Error("No agency selected !!!");
                return RedirectToAction(nameof(Agencies));
            }

            var result = await _companyAgencyService.EmpanelAgenciesAsync(userEmail, vendors);

            if (result.Success)
            {
                _notifyService.Custom(result.Message, 3, "green", "fas fa-thumbs-up");
            }
            else
            {
                _notifyService.Error(result.Message);
            }

            return RedirectToAction(nameof(Agencies));
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    return RedirectToAction(nameof(Agencies));
                }

                var vendor = await _manageAgencyService.GetVendorAsync(userEmail, id);
                if (vendor == null)
                {
                    _notifyService.Error("Error getting Agency. Try again.");
                    return RedirectToAction(nameof(Agencies));
                }

                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyActionPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit Agency", nameof(Edit));

                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Agency {Id}. {UserEmail}", id, userEmail);
                _notifyService.Error("Error getting User. Try again.");
                return RedirectToAction(nameof(Agencies));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _notifyService.Error("Please correct the errors");
                    await _manageAgencyService.LoadModel(model);
                    return View(model);
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await _manageAgencyService.EditAsync(userEmail, model, _baseUrl);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageAgencyService.LoadModel(model);
                    return View(model);
                }
                _notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {AgencyId}. {UserEmail}.", vendorId, User?.Identity?.Name);
                _notifyService.Error("Error editing Agency. Try again.");
            }
            return RedirectToAction(nameof(Detail), ControllerName<AvailableAgencyController>.Name, new { id = vendorId });
        }

        [Breadcrumb("Agency Profile", FromAction = nameof(Agencies))]
        public async Task<IActionResult> Detail(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    _notifyService.Error("Error getting Agency");
                    return RedirectToAction(nameof(Agencies));
                }

                var vendor = await _manageAgencyService.GetVendorDetailAsync(id);
                if (vendor == null)
                {
                    _notifyService.Error("Error getting Agency");
                    return RedirectToAction(nameof(Agencies));
                }

                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error getting Agency. Try again.");
                return RedirectToAction(nameof(Agencies));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            if (id < 1)
                return BadRequest(new { message = "Invalid Agency Id." });

            var userEmail = User.Identity?.Name;
            var (success, message) = await _manageAgencyService.SoftDeleteAgencyAsync(id, userEmail);

            if (!success)
            {
                // Check if it was a "Not Found" error vs a "Server Error"
                if (message.Contains("not found"))
                    return NotFound(new { message });

                return StatusCode(500, new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
    }
}