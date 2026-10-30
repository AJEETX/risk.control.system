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
    public class ActiveAgencyUserController(IManageAgencyUserService manageAgencyUserService,
            IErrorNotifyService errorNotifyService,
            INavigationService navigationService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
             IEncryptionService encryptionService,
            ILogger<ActiveAgencyUserController> logger) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IManageAgencyUserService _manageAgencyUserService = manageAgencyUserService;
        private readonly IErrorNotifyService _errorNotifyService = errorNotifyService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly ILogger<ActiveAgencyUserController> _logger = logger;
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Users([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var model = new ServiceModel
            {
                Id = id
            };
            var encryptedId = _encryptionService.ProtectId(id);
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserManagerPath(encryptedId, ControllerName<ActiveAgencyController>.Name, "Active Agencies");
            return View(model);
        }

        public async Task<IActionResult> Create([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id <= 0)
            {
                _notifyService.Error("OOPS !!!..Error creating user");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
            var model = await _manageAgencyUserService.GetUserCreationModelAsync(id);
            var encryptedId = _encryptionService.ProtectId(id);
            if (model == null)
            {
                _notifyService.Error("OOPS !!!..Agency Not Found. Try again.");
                return RedirectToAction(nameof(Users), new { id = encryptedId });
            }
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(encryptedId, ControllerName<ActiveAgencyController>.Name, "Active Agencies", "Add User", "Create");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string emailSuffix)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _manageAgencyUserService.LoadModelAsync(model);
                    return View(model);
                }

                var (Success, Message, Errors) = await _manageAgencyUserService.CreateAgencyUserAsync(ModelState, model, emailSuffix, userEmail, _baseUrl);

                if (!Success)
                {
                    foreach (var error in Errors)
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

            var encryptedId = _encryptionService.ProtectId(model.VendorId!.Value);
            return RedirectToAction(nameof(Users), ControllerName<ActiveAgencyUserController>.Name, new { id = model.VendorId });
        }

        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id <= 0)
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }

                var agencyUser = await _manageAgencyUserService.GetUserForEditAsync(id);

                if (agencyUser == null)
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }

                var encryptedId = _encryptionService.ProtectId(agencyUser.VendorId!.Value);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(encryptedId, ControllerName<ActiveAgencyController>.Name, "Active Agencies", "Edit User", "Edit");

                return View(agencyUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, ApplicationUser model)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                model.Id = id;
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
            var encryptedId = _encryptionService.ProtectId(model.VendorId!.Value);
            return RedirectToAction(nameof(Users), ControllerName<ActiveAgencyUserController>.Name, new { id = encryptedId });
        }

        public async Task<IActionResult> Delete([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id < 1)
                {
                    _notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }

                var model = await _manageAgencyUserService.GetUserForDeleteAsync(id);
                if (model == null)
                {
                    _notifyService.Error("OOPS!!!. User Not Found.");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }

                var encryptedId = _encryptionService.ProtectId(model.VendorId!.Value);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(encryptedId, ControllerName<ActiveAgencyController>.Name, "Active Agencies", "Delete User", "Edit");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error getting user. Try again");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, long vendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("Error occurred. Please Try again later");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
            try
            {
                var (result, message) = await _manageAgencyUserService.SoftDeleteUserAsync(id, userEmail);

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
                _logger.LogError(ex, "Error deleting {AgencyUser}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error deleting user. Try again");
            }

            var encryptedId = _encryptionService.ProtectId(vendorId);
            return RedirectToAction(nameof(Users), new { id = encryptedId });
        }
    }
}