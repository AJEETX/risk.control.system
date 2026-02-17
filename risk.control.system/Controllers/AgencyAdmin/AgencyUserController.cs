using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.AgencyAdmin
{
    [Breadcrumb("Admin Settings ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAgencyUserCreateEditService agencyUserCreateEditService;
        private readonly INotyfService notifyService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<AgencyUserController> logger;
        private string portal_base_url = string.Empty;

        public AgencyUserController(ApplicationDbContext context,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            INotyfService notifyService,
            IFeatureManager featureManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyUserController> logger)
        {
            _context = context;
            this.agencyUserCreateEditService = agencyUserCreateEditService;
            this.notifyService = notifyService;
            this.featureManager = featureManager;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Users));
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            return View();
        }

        [Breadcrumb("Add User")]
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }
                var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Users));
                }
                var availableRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude MANAGER if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
                var model = new ApplicationUser
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor,
                    AvailableRoles = availableRoles
                };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency for {UserEmail}", userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Error creating user. Try again.");
                return RedirectToAction(nameof(Users));
            }
        }

        private async Task LoadModel(ApplicationUser model)
        {
            var availableRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude MANAGER if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.Vendor = vendor;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = availableRoles;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string emailSuffix, string vendorId)
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency {Id} user. {UserEmail}", vendorId, userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Error Creating User. Try again.");
            }
            return RedirectToAction(nameof(Users));
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> Edit(long? id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id == null || id <= 0)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }

                var user = await _context.ApplicationUser.AsNoTracking().Include(u => u.Country).Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Id == id);
                if (user == null)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(Users));
                }
                user.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !user.IsPasswordChangeRequired : true;

                return View(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting AgencyUser {Id}. {UserEmail}", id, userEmail ?? "Anonymous");
            }
            notifyService.Error("OOPs !!!.Error creating User. Try again");
            return RedirectToAction(nameof(Users));
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
                    UpdatedBy = userEmail
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
                logger.LogError(ex, "Error getting AgencyUser {Id}. {UserEmail}", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPS !!!..Error editing User. Try again.");
            }
            return RedirectToAction(nameof(Users));
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> Delete(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPS!!!.Invalid Data.Try Again");
                    return RedirectToAction(nameof(Users));
                }
                var model = await _context.ApplicationUser.AsNoTracking().Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == id);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.User Not Found.Try Again");
                    return RedirectToAction(nameof(Users));
                }

                var agencySubStatuses = new[] {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                var hasClaims = _context.Investigations.AsNoTracking().Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting AgencyUser {UserId} for {UserEmail}", id, userEmail ?? "Anonymous");
                notifyService.Error("OOPS!!!..Error deleting user. Try again");
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string email)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(Users));
                }
                var model = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(Users));
                }

                model.Updated = DateTime.UtcNow;
                model.UpdatedBy = userEmail;
                model.Deleted = true;
                _context.ApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b> {model.Email}</b> deleted successfully", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(Users), "AgencyUser", new { id = model.VendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting AgencyUser {email}. {UserEmail}", email, userEmail);
                notifyService.Error("OOPS!!!..Error deleting user. Try again");
                return RedirectToAction(nameof(Users));
            }
        }
    }
}