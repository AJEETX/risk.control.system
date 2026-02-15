using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Manager
{
    [Breadcrumb("Manage Agency")]
    [Authorize(Roles = $"{MANAGER.DISPLAY_NAME}")]
    public class AvailableAgencyUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyUserCreateEditService agencyUserCreateEditService;
        private readonly INotyfService notifyService;
        private readonly INavigationService navigationService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<AvailableAgencyUserController> logger;
        private readonly string portal_base_url = string.Empty;

        public AvailableAgencyUserController(
            ApplicationDbContext context,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            INotyfService notifyService,
            INavigationService navigationService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<AvailableAgencyUserController> logger)
        {
            _context = context;
            this.agencyUserCreateEditService = agencyUserCreateEditService;
            this.notifyService = notifyService;
            this.navigationService = navigationService;
            this.featureManager = featureManager;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
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

            ViewData["BreadcrumbNode"] = navigationService.GetAgencyUserManagerPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies");

            return View(model);
        }

        public async Task<IActionResult> Create(long id)
        {
            if (id <= 0)
            {
                notifyService.Custom($"OOPs !!!..Error creating user.", 3, "red", "fa fa-user");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            List<SelectListItem> allRoles = null;
            AppRoles? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            var currentVendorUserCount = await _context.ApplicationUser.CountAsync(v => v.VendorId == id);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AppRoles.AGENCY_ADMIN;
                status = true;
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r == AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            else
            {
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            var model = new ApplicationUser
            {
                Active = status,
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                AvailableRoles = allRoles,
                Role = role
            };

            ViewData["BreadcrumbNode"] = navigationService.GetAgencyUserActionPath(id, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Add User", "Create");
            return View(model);
        }

        private async Task LoadModel(ApplicationUser model)
        {
            List<SelectListItem> allRoles = null;
            AppRoles? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

            var currentVendorUserCount = await _context.ApplicationUser.CountAsync(v => v.VendorId == model.VendorId);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AppRoles.AGENCY_ADMIN;
                status = true;
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r == AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            else
            {
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            model.Active = status;
            model.Vendor = vendor;
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = allRoles;
            model.Role = role;
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
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                model.Id = 0; // Ensure Id is 0 for new user
                var vendorUserModel = new CreateVendorUserRequest
                {
                    User = model,
                    EmailSuffix = emailSuffix,
                    CreatedBy = userEmail
                };
                var result = await agencyUserCreateEditService.CreateVendorUserAsync(vendorUserModel, ModelState, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    notifyService.Error(result.Message);
                    await LoadModel(model);
                    return View(model);
                }
                return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = model.VendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Creating User. {UserEmail}.", userEmail);
                notifyService.Error("OOPS !!!..Error Creating User. Try again.");
                return RedirectToAction(nameof(Create), ControllerName<AvailableAgencyUserController>.Name, new { id = model.VendorId });
            }
        }

        public async Task<IActionResult> Edit(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await _context.ApplicationUser.Include(v => v.Country)?.Include(v => v.Vendor)?.FirstOrDefaultAsync(v => v.Id == id);
                if (model == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                model.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !model.IsPasswordChangeRequired : true;

                ViewData["BreadcrumbNode"] = navigationService.GetAgencyUserActionPath(model.VendorId.Value, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Edit User", "Edit");

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {UserId}. {UserEmail}.", id, userEmail);
                notifyService.Error("Error getting User. Try again.");
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
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                var result = await agencyUserCreateEditService.EditVendorUserAsync(new EditVendorUserRequest
                {
                    UserId = id,
                    Model = model,
                    UpdatedBy = User?.Identity?.Name
                },
                ModelState, portal_base_url);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);
                    notifyService.Error(result.Message);
                    await LoadModel(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing {UserId}. {UserEmail}.", id, userEmail);
                notifyService.Error("Error editing User. Try again.");
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
                    notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }
                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == id);
                if (model == null)
                {
                    notifyService.Error("Error getting User. Try again.");
                    return RedirectToAction(nameof(Users));
                }

                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                model.HasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);

                ViewData["BreadcrumbNode"] = navigationService.GetAgencyUserActionPath(model.VendorId.Value, ControllerName<AvailableAgencyController>.Name, "Available Agencies", "Delete User", "Delete");

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {UserId}. {UserEmail}.", id, userEmail);
                notifyService.Error("Error deleting User. Try again.");
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string email, long vendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            if (!ModelState.IsValid)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            try
            {
                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                model.Updated = DateTime.UtcNow;
                model.UpdatedBy = userEmail;
                model.Deleted = true;
                _context.ApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b>{model.Email}</b> Deleted successfully", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(Users), ControllerName<AvailableAgencyUserController>.Name, new { id = vendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {UserId}. {UserEmail}.", email, userEmail);
                notifyService.Error("Error deleting User. Try again.");
                return RedirectToAction(nameof(Users));
            }
        }
    }
}