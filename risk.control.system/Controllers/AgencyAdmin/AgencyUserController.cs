using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyUserController : Controller
    {
        private readonly IAgencyAdminUserService _service;
        private readonly IAgencyUserCreateEditService _createEditService;
        private readonly IErrorNotifyService _errorNotifyService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<AgencyUserController> _logger;
        private readonly string _baseUrl;

        public AgencyUserController(
            IAgencyAdminUserService service,
            IAgencyUserCreateEditService createEditService,
            IErrorNotifyService errorNotifyService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyUserController> logger)
        {
            _service = service;
            _createEditService = createEditService;
            _notifyService = notifyService;
            _logger = logger;
            _errorNotifyService = errorNotifyService;

            var req = httpContextAccessor?.HttpContext?.Request;
            _baseUrl = $"{req?.Scheme}://{req?.Host.ToUriComponent()}{req?.PathBase.ToUriComponent()}";
        }

        public IActionResult Index() => RedirectToAction(nameof(Users));

        [Breadcrumb("Manage Users")]
        public IActionResult Users() => View();

        [Breadcrumb("Add User")]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;
            try
            {
                var model = await _service.PrepareCreateModelAsync(userEmail);
                if (model == null)
                {
                    _notifyService.Error("User or Agency not found.");
                    return RedirectToAction(nameof(Users));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create User view.");
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string emailSuffix)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _service.LoadMetadataAsync(model);
                    return View(model);
                }
                emailSuffix = emailSuffix.Replace("\n", "").Replace("\r", "").Trim();
                var userEmail = HttpContext.User?.Identity?.Name!;
                var result = await _createEditService.CreateUserAsync(new CreateVendorUserRequest
                {
                    User = model,
                    EmailSuffix = emailSuffix,
                    CreatedBy = userEmail
                }, ModelState, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _service.LoadMetadataAsync(model);
                    return View(model);
                }

                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Agency User");
                return RedirectToAction(nameof(Users));
            }
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            try
            {
                var model = await _service.PrepareEditModelAsync(id);
                if (model == null)
                {
                    _notifyService.Error("User not found.");
                    return RedirectToAction(nameof(Users));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error to get Agency user {UserId}", id);
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, ApplicationUser model)
        {
            try
            {
                model.Id = id;
                if (!ModelState.IsValid)
                {
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _service.LoadMetadataAsync(model);
                    return View(model);
                }
                var userEmail = HttpContext.User?.Identity?.Name!;
                var result = await _createEditService.EditUserAsync(new EditVendorUserRequest
                {
                    UserId = id,
                    Model = model,
                    UpdatedBy = userEmail
                }, ModelState, _baseUrl);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    _errorNotifyService.ShowErrorNotification(ModelState);
                    await _service.LoadMetadataAsync(model);
                    return View(model);
                }

                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing user {UserId}", id);
                _errorNotifyService.ShowErrorNotification(ModelState);
                await _service.LoadMetadataAsync(model);
                return View(model);
            }
        }

        [Breadcrumb("Delete", FromAction = "Users")]
        public async Task<IActionResult> Delete([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            try
            {
                var model = await _service.GetUserForDeleteAsync(id);
                if (model == null)
                {
                    _notifyService.Error("User Not Found.");
                    return RedirectToAction(nameof(Users));
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                _notifyService.Error($"Error getting user to delete.");
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name!;
                var (deleted, username) = await _service.SoftDeleteUserAsync(id, userEmail);
                if (deleted)
                {
                    _notifyService.Custom($"User {username} deleted.", 3, "red", "fas fa-user-minus");
                }
                else
                {
                    _notifyService.Error($"User {username} delete  failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleteing user {UserId}", id);
                _notifyService.Error($"Error deleteing user.");
            }
            return RedirectToAction(nameof(Users));
        }
    }
}