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
using risk.control.system.Services.Manager;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyUserController(
            IErrorNotifyService errorNotifyService,
            IEncryptionService encryptionService,
            IManageAgencyUserService manageAgencyUserService,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            INotyfService notifyService,
            INavigationService navigationService,
             IHttpContextAccessor httpContextAccessor,
            ILogger<AvailableAgencyUserController> logger) : Controller
    {
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly IErrorNotifyService _errorNotifyService = errorNotifyService;
        private readonly IManageAgencyUserService _manageAgencyUserService = manageAgencyUserService;
        private readonly IAgencyUserCreateEditService _agencyUserCreateEditService = agencyUserCreateEditService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly INavigationService _navigationService = navigationService;
        private readonly ILogger<AvailableAgencyUserController> _logger = logger;
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

            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserManagerPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies");

            return View(model);
        }

        public async Task<IActionResult> Create([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id <= 0)
            {
                _notifyService.Custom($"OOPs !!!..Error creating user.", 3, "red", "fa fa-user");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }

            var model = await _manageAgencyUserService.GetNewUserCreationModelAsync(id);
            if (model == null)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name);
            }
            var encryptedId = _encryptionService.ProtectId(id);
            ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Add User", "Create");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string emailSuffix)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            emailSuffix = emailSuffix.Replace("\n", "").Replace("\r", "").Trim();
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
                _logger.LogError(ex, "Error Creating User. {UserEmail}.", userEmail);
                _notifyService.Error("OOPS !!!..Error Creating User. Try again.");
            }
            var encryptedId = _encryptionService.ProtectId(model.VendorId!.Value);
            return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = encryptedId });
        }

        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                _notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
            try
            {
                var agencyUser = await _manageAgencyUserService.GetUserForEditAsync(id);

                if (agencyUser == null)
                {
                    _notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }
                var encryptedId = _encryptionService.ProtectId(agencyUser.VendorId!.Value);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit User", "Edit");

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
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, ApplicationUser model)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                model.Id = id;
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);

                    await _manageAgencyUserService.LoadEditModelAsync(model);
                    return View(model);
                }
                var (Success, Message, Errors) = await _agencyUserCreateEditService.EditUserAsync(new EditVendorUserRequest
                {
                    UserId = id,
                    Model = model,
                    UpdatedBy = User?.Identity?.Name!
                },
                ModelState, _baseUrl);

                if (!Success)
                {
                    foreach (var error in Errors)
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
            var encryptedId = _encryptionService.ProtectId(model.VendorId!.Value);
            return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = encryptedId });
        }

        public async Task<IActionResult> Delete([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
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
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }

                var encryptedId = _encryptionService.ProtectId(model.VendorId!.Value);
                ViewData["BreadcrumbNode"] = _navigationService.GetAgencyUserActionPath(encryptedId, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Delete User", "Delete");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {UserId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error deleting User. Try again.");
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
                _logger.LogError(ex, "Error deleting {UserId}. {UserEmail}.", id, userEmail);
                _notifyService.Error("Error deleting User. Try again.");
            }
            var encryptedId = _encryptionService.ProtectId(vendorId);
            return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = encryptedId });
        }
    }
}