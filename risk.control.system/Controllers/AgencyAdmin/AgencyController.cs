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
    public class AgencyController : Controller
    {
        private readonly IAgencyProfileService _profileService;
        private readonly IManageAgencyService _editService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<AgencyController> _logger;
        private readonly string _portalBaseUrl;

        public AgencyController(
            IAgencyProfileService profileService,
            IManageAgencyService editService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyController> logger)
        {
            _profileService = profileService;
            _editService = editService;
            _notifyService = notifyService;
            _logger = logger;

            var request = httpContextAccessor?.HttpContext?.Request;
            _portalBaseUrl = $"{request?.Scheme}://{request?.Host.ToUriComponent()}{request?.PathBase.ToUriComponent()}";
        }

        public IActionResult Index() => RedirectToAction(nameof(Profile));

        [Breadcrumb("Agency Profile ", FromAction = nameof(Index))]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.Identity?.Name;
            try
            {
                var vendor = await _profileService.GetAgencyProfileAsync(userEmail);
                if (vendor == null)
                {
                    _notifyService.Error("Agency Not Found!");
                    return RedirectToAction("Index", "Dashboard");
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for {UserEmail}", userEmail);
                _notifyService.Error("An error occurred. Contact Admin.");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [Breadcrumb("Edit Agency", FromAction = nameof(Profile))]
        public async Task<IActionResult> Edit()
        {
            var userEmail = User.Identity?.Name;
            try
            {
                var vendor = await _profileService.GetAgencyForEditAsync(userEmail);
                if (vendor == null)
                {
                    _notifyService.Error("Agency or User not found.");
                    return RedirectToAction("Index", "Dashboard");
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit mode for {UserEmail}", userEmail);
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor model)
        {
            var userEmail = User.Identity?.Name;
            if (!ModelState.IsValid)
            {
                await _profileService.LoadAgencyMetadataAsync(model, userEmail);
                return View(model);
            }

            try
            {
                var result = await _editService.EditAsync(userEmail, model, _portalBaseUrl);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    await _profileService.LoadAgencyMetadataAsync(model, userEmail);
                    return View(model);
                }

                _notifyService.Custom($"Agency edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Agency for {UserEmail}", userEmail);
                _notifyService.Error("Error Editing Agency. Try again.");
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}