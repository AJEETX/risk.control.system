using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;
using risk.control.system.Services.Manager;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyUserController : Controller
    {
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly IManageAgencyUserService _manageAgencyUserService;
        private readonly IAgencyUserCreateEditService _agencyUserCreateEditService;
        private readonly INotyfService _notifyService;
        private readonly INavigationService _navigationService;
        private readonly IFeatureManager _featureManager;
        private readonly ILogger<AvailableAgencyUserController> _logger;
        private readonly string _baseUrl;

        public AvailableAgencyUserController(
            IErrorNotifyService errorNotifyService,
            IManageAgencyUserService manageAgencyUserService,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            INotyfService notifyService,
            INavigationService navigationService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<AvailableAgencyUserController> logger)
        {
            _errorNotifyService = errorNotifyService;
            _manageAgencyUserService = manageAgencyUserService;
            _agencyUserCreateEditService = agencyUserCreateEditService;
            _notifyService = notifyService;
            _navigationService = navigationService;
            _featureManager = featureManager;
            _logger = logger;
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

            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserManagerPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies");

            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            if (id <= 0)
            {
                _notifyService.Custom($"OOPs !!!..Error creating user.", 3, "red", "fa fa-user");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }

            var model = await _manageAgencyUserService.GetNewUserCreationModelAsync(id);
            if (model == null)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Add User", "Create");
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

                return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = model.VendorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Creating User. {UserEmail}.", userEmail);
                _notifyService.Error("OOPS !!!..Error Creating User. Try again.");
                return RedirectToAction(nameof(Create), ControllerName<AvailableAgencyUserController>.Name, new { id = model.VendorId });
            }
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var agencyUser = await _manageAgencyUserService.GetUserForEditAsync(id);

                if (agencyUser == null)
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(agencyUser.VendorId.Value, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit User", "Edit");

                return View(agencyUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {UserId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error getting User. Try again.");
                return RedirectToAction(nameof(Users));
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

                    await _manageAgencyUserService.LoadEditModelAsync(model);
                    return View(model);
                }
                var result = await _agencyUserCreateEditService.EditVendorUserAsync(new EditVendorUserRequest
                {
                    UserId = id,
                    Model = model,
                    UpdatedBy = User?.Identity?.Name
                },
                ModelState, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _manageAgencyUserService.LoadEditModelAsync(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing {UserId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error editing User. Try again.");
            }
            return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = model.VendorId });
        }

        public async Task<IActionResult> Delete(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (!ModelState.IsValid)
                {
                    _notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }
                var model = await _manageAgencyUserService.GetUserForDeleteAsync(id);
                if (model == null)
                {
                    _notifyService.Error("OOPS!!!. User Not Found.");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(model.VendorId.Value, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Delete User", "Delete");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {UserId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error deleting User. Try again.");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string email, long vendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
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
                _logger.LogError(ex, "Error deleting {UserId}. {UserEmail}.", email, userEmail);
                _notifyService.Error("Error deleting User. Try again.");
            }
            return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = vendorId });
        }
    }
}