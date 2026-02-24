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
    public class EmpanelledAgencyController : Controller
    {
        private readonly IManageAgencyService _manageAgencyService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly IManageAgencyService _agencyCreateEditService;
        private readonly INavigationService _navigationService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<EmpanelledAgencyController> _logger;
        private readonly string _baseUrl;
        private readonly ICompanyAgencyService _companyAgencyService;

        public EmpanelledAgencyController(
            IManageAgencyService manageAgencyService,
            IErrorNotifyService errorNotifyService,
            IManageAgencyService agencyCreateEditService,
            ICompanyAgencyService companyAgencyService,
            INavigationService navigationService,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyController> logger)
        {
            _manageAgencyService = manageAgencyService;
            this._errorNotifyService = errorNotifyService;
            _companyAgencyService = companyAgencyService;
            _agencyCreateEditService = agencyCreateEditService;
            _navigationService = navigationService;
            _notifyService = notifyService;
            _logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

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
            var userEmail = User.Identity?.Name;

            // Basic validation
            if (!ModelState.IsValid || vendors == null || vendors.Count == 0)
            {
                _notifyService.Error("No agency selected !!!");
                return RedirectToAction(nameof(Agencies));
            }

            var result = await _companyAgencyService.DepanelAgenciesAsync(userEmail, vendors);

            if (result.Success)
            {
                _notifyService.Custom(result.Message, 3, "orange", "far fa-hand-pointer");
            }
            else
            {
                _notifyService.Error(result.Message);
            }

            return RedirectToAction(nameof(Agencies));
        }

        [Breadcrumb("Agency Profile", FromAction = nameof(Agencies))]
        public async Task<IActionResult> Detail(long id)
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
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyActionPath(id, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Edit Agency", nameof(Edit));
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
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _notifyService.Error("Please correct the errors");
                    await _manageAgencyService.LoadModel(model);
                    return View(model);
                }

                var result = await _agencyCreateEditService.EditAsync(userEmail, model, _baseUrl);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState); ;
                    await _manageAgencyService.LoadModel(model);
                    return View(model);
                }
                _notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {AgencyId} for {UserEmail}.", vendorId, userEmail);
                _notifyService.Error("Error editing agency. Try again.");
            }
            return RedirectToAction(nameof(Detail), ControllerName<EmpanelledAgencyController>.Name, new { id = vendorId });
        }

        private void ShowErrorNotification()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct();
            _notifyService.Error($"<b>Please fix:</b><br/>{string.Join("<br/>", errors)}");
        }
    }
}