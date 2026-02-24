using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Manager;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class EmpanelledAgencyUserController : Controller
    {
        private readonly IManageAgencyUserService _manageAgencyUserService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly INotyfService _notifyService;
        private readonly INavigationService _navigationService;
        private readonly IFeatureManager _featureManager;
        private readonly ILogger<EmpanelledAgencyUserController> _logger;
        private readonly string _baseUrl;

        public EmpanelledAgencyUserController(
            IManageAgencyUserService manageAgencyUserService,
            IErrorNotifyService errorNotifyService,
            INavigationService navigationService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyUserController> logger,
            ISmsService SmsService)
        {
            this._manageAgencyUserService = manageAgencyUserService;
            this._errorNotifyService = errorNotifyService;
            this._navigationService = navigationService;
            this._notifyService = notifyService;
            this._featureManager = featureManager;
            this._logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Users(long id)
        {
            var model = new ServiceModel
            {
                Id = id
            };

            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserManagerPath(id, ControllerName<EmpanelledAgencyController>.Name, "Available Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            if (id <= 0)
            {
                _notifyService.Error("OOPS !!!..Error creating user");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var model = await _manageAgencyUserService.GetUserCreationModelAsync(id);
            if (model == null)
            {
                _notifyService.Error("OOPS !!!..Agency Not Found. Try again.");
                return RedirectToAction(nameof(Users), new { id = id });
            }
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(id, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Add User", "Create");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string emailSuffix)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageAgencyUserService.LoadModelAsync(model);
                    return View(model);
                }

                var result = await _manageAgencyUserService.CreateAgencyUserAsync(ModelState, model, emailSuffix, userEmail, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _manageAgencyUserService.LoadModelAsync(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Creating AgencyUser {Id}. {UserEmail}.", model.Id, userEmail);
                _notifyService.Error("OOPS !!!..Error Creating User. Try again.");
            }
            return RedirectToAction(nameof(Users), ControllerName<EmpanelledAgencyUserController>.Name, new { id = model.VendorId });
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var agencyUser = await _manageAgencyUserService.GetUserForEditAsync(id);

                if (agencyUser == null)
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(agencyUser.VendorId.Value, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Edit User", "Edit");

                return View(agencyUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error getting user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageAgencyUserService.LoadModelAsync(model);
                    return View(model);
                }

                var result = await _manageAgencyUserService.EditAgencyUserAsync(ModelState, id, model, userEmail, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _manageAgencyUserService.LoadModelAsync(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {AgencyId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("OOPS !!!..Error editing User. Try again.");
            }
            return RedirectToAction(nameof(Users), ControllerName<EmpanelledAgencyUserController>.Name, new { id = model.VendorId });
        }

        public async Task<IActionResult> Delete(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id < 1)
                {
                    _notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await _manageAgencyUserService.GetUserForDeleteAsync(id);
                if (model == null)
                {
                    _notifyService.Error("OOPS!!!. User Not Found.");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(model.VendorId.Value, ControllerName<EmpanelledAgencyController>.Name, "Active Agencies", "Edit User", "Edit");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error getting user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string email, long vendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var (result, message) = await _manageAgencyUserService.SoftDeleteUserAsync(email, User.Identity?.Name);

                if (result)
                {
                    _notifyService.Custom(message, 3, "orange", "fas fa-user-minus");
                }
                else
                {
                    _notifyService.Error(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {AgencyUser}. {UserEmail}.", email, userEmail);
                _notifyService.Error("Error deleting user. Try again");
            }
            return RedirectToAction(nameof(Users), new { id = vendorId });
        }
    }
}