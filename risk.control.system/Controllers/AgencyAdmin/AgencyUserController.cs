using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyUserController : Controller
    {
        private readonly IAgencyAdminUserService _service;
        private readonly IAgencyUserCreateEditService _createEditService;
        private readonly INotyfService _notifyService;
        private readonly ILogger<AgencyUserController> _logger;
        private readonly string _baseUrl;

        public AgencyUserController(
            IAgencyAdminUserService service,
            IAgencyUserCreateEditService createEditService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyUserController> logger)
        {
            _service = service;
            _createEditService = createEditService;
            _notifyService = notifyService;
            _logger = logger;

            var req = httpContextAccessor?.HttpContext?.Request;
            _baseUrl = $"{req?.Scheme}://{req?.Host.ToUriComponent()}{req?.PathBase.ToUriComponent()}";
        }

        public IActionResult Index() => RedirectToAction(nameof(Users));

        [Breadcrumb("Manage Users")]
        public IActionResult Users() => View();

        [Breadcrumb("Add User")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = await _service.PrepareCreateModelAsync(User.Identity?.Name);
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
            if (!ModelState.IsValid)
            {
                await _service.LoadMetadataAsync(model);
                return View(model);
            }

            var result = await _createEditService.CreateVendorUserAsync(new CreateVendorUserRequest
            {
                User = model,
                EmailSuffix = emailSuffix,
                CreatedBy = User.Identity?.Name
            }, ModelState, _baseUrl);

            if (!result.Success)
            {
                _notifyService.Error(result.Message);
                await _service.LoadMetadataAsync(model);
                return View(model);
            }

            return RedirectToAction(nameof(Users));
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> Edit(long id)
        {
            var model = await _service.PrepareEditModelAsync(id);
            if (model == null)
            {
                _notifyService.Error("User not found.");
                return RedirectToAction(nameof(Users));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                await _service.LoadMetadataAsync(model);
                return View(model);
            }

            var result = await _createEditService.EditVendorUserAsync(new EditVendorUserRequest
            {
                UserId = id,
                Model = model,
                UpdatedBy = User.Identity?.Name
            }, ModelState, _baseUrl);

            if (!result.Success)
            {
                _notifyService.Error(result.Message);
                await _service.LoadMetadataAsync(model);
                return View(model);
            }

            return RedirectToAction(nameof(Users));
        }

        [Breadcrumb("Delete", FromAction = "Users")]
        public async Task<IActionResult> Delete(long id)
        {
            var model = await _service.GetUserForDeleteAsync(id);
            if (model == null)
            {
                _notifyService.Error("User Not Found.");
                return RedirectToAction(nameof(Users));
            }
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string email)
        {
            var success = await _service.SoftDeleteUserAsync(email, User.Identity?.Name);
            if (success)
            {
                _notifyService.Custom($"User <b> {email} </b> deleted.", 3, "red", "fas fa-user-minus");
            }
            else
            {
                _notifyService.Error($"Delete  <b> {email} </b>failed.");
            }
            return RedirectToAction(nameof(Users));
        }
    }
}