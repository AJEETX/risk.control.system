using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class ActiveAgencyController(
            IManageAgencyService manageAgencyService,
            IEncryptionService encryptionService,
            IErrorNotifyService errorNotifyService,
            IManageAgencyService agencyCreateEditService,
            ICompanyAgencyService companyAgencyService,
            INavigationService navigationService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<ActiveAgencyController> logger) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IManageAgencyService _manageAgencyService = manageAgencyService;
        private readonly IErrorNotifyService _errorNotifyService = errorNotifyService;
        private readonly IManageAgencyService _agencyCreateEditService = agencyCreateEditService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ILogger<ActiveAgencyController> _logger = logger;
        private readonly ICompanyAgencyService _companyAgencyService = companyAgencyService;
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Agencies));
        }

        [Breadcrumb("Active Agencies")]
        public IActionResult Agencies()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agencies(List<long> vendors)
        {
            var userEmail = User.Identity?.Name!;

            // Basic validation
            if (!ModelState.IsValid || vendors == null || vendors.Count == 0)
            {
                _notifyService.Error("No agency selected !!!");
                return RedirectToAction(nameof(Agencies));
            }

            var (Success, Message) = await _companyAgencyService.DepanelAgenciesAsync(userEmail, vendors);

            if (Success)
            {
                _notifyService.Custom(Message, 3, "orange", "far fa-hand-pointer");
            }
            else
            {
                _notifyService.Error(Message);
            }

            return RedirectToAction(nameof(Agencies));
        }

        [Breadcrumb("Agency Profile", FromAction = nameof(Agencies))]
        public async Task<IActionResult> Profile([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
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
                _logger.LogError(ex, "Error getting {AgencyId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                _notifyService.Error("Error getting Agency. Try again");
                return RedirectToAction(nameof(Agencies));
            }
        }

        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
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
                var encryptedId = _encryptionService.ProtectId(id);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyActionPath(encryptedId, ControllerName<ActiveAgencyController>.Name, "Active Agencies", "Edit Agency", nameof(Edit));
                return View(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId} for {UserEmail}.", id, HttpContext.User?.Identity?.Name);
                _notifyService.Error("Agency Not Found. Try again.");
                return RedirectToAction(nameof(Agencies));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid)
                {
                    _notifyService.Error("Please correct the errors");
                    await _manageAgencyService.LoadModel(model);
                    return View(model);
                }

                var (Success, Errors) = await _agencyCreateEditService.EditAsync(userEmail, model, _baseUrl);
                if (!Success)
                {
                    foreach (var error in Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState); ;
                    await _manageAgencyService.LoadModel(model);
                    return View(model);
                }
                _notifyService.Custom($"Agency <b>{model.Email}</b> profile edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {AgencyId} for {UserEmail}.", vendorId, userEmail);
                _notifyService.Error("Error editing agency. Try again.");
            }
            var encryptedId = _encryptionService.ProtectId(vendorId);
            return RedirectToAction(nameof(Profile), ControllerName<ActiveAgencyController>.Name, new { id = encryptedId });
        }
    }
}