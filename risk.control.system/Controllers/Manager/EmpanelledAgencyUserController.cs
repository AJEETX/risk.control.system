using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AspNetCoreHero.ToastNotification.Abstractions;
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
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Manager
{
    public class EmpanelledAgencyUserController : Controller
    {
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly IAgencyUserCreateEditService agencyUserCreateEditService;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<EmpanelledAgencyUserController> logger;
        private readonly string portal_base_url;

        public EmpanelledAgencyUserController(ApplicationDbContext context,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<EmpanelledAgencyUserController> logger,
            ISmsService SmsService)
        {
            this._context = context;
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
            return View();
        }

        [Breadcrumb(" Manage Users", FromAction = nameof(EmpanelledAgencyController.Detail), FromController = typeof(EmpanelledAgencyController))]
        public IActionResult Users(long id)
        {
            var model = new ServiceModel
            {
                Id = id
            };

            var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
            var detailsPage = new MvcBreadcrumbNode("Detail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Users", "EmpanelledAgency", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        [Breadcrumb(" Add User", FromAction = "Users")]
        public async Task<IActionResult> Create(long id)
        {
            if (id <= 0)
            {
                notifyService.Error("OOPS !!!..Error creating user");
                return this.RedirectToAction<DashboardController>(x => x.Index());
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
            var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Agency Not Found. Try again.");
                return RedirectToAction(nameof(Users), "EmpanelledAgencyUser", new { id = id });
            }

            var model = new ApplicationUser
            {
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                AvailableRoles = availableRoles
            };

            var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
            var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
            var detailsPage = new MvcBreadcrumbNode("Detail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Users", "EmpanelledAgencyUser", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = id } };
            var addPage = new MvcBreadcrumbNode("Create", "EmpanelledAgencyUser", $"Add user") { Parent = editPage };
            ViewData["BreadcrumbNode"] = addPage;

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
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                model.Id = 0; // Ensure Id is 0 for new user
                var vendorUserModel = new CreateVendorUserRequest
                {
                    User = model,
                    EmailSuffix = emailSuffix,
                    CreatedBy = User?.Identity?.Name
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
                logger.LogError(ex, "Error Creating AgencyUser {Id}. {UserEmail}.", model.Id, userEmail);
                notifyService.Error("OOPS !!!..Error Creating User. Try again.");
            }
            return RedirectToAction(nameof(Users), "EmpanelledAgencyUser", new { id = model.VendorId });
        }

        private async Task LoadModel(ApplicationUser model)
        {
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
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
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.Vendor = vendor;
            model.AvailableRoles = availableRoles;
        }

        [Breadcrumb(" Edit User", FromAction = "Users")]
        public async Task<IActionResult> Edit(long? userId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (userId == null || userId <= 0)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var vendorApplicationUser = await _context.ApplicationUser.Include(u => u.Vendor).Include(v => v.Country).FirstOrDefaultAsync(v => v.Id == userId);

                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
                vendorApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !vendorApplicationUser.IsPasswordChangeRequired : true;

                var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Detail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = vendorApplicationUser.VendorId } };
                var editPage = new MvcBreadcrumbNode("Users", "EmpanelledAgencyUser", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = vendorApplicationUser.VendorId } };
                var addPage = new MvcBreadcrumbNode("Edit", "EmpanelledAgencyUser", $"Edit user") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;
                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", userId, userEmail);
                notifyService.Error("Error getting user. Try again");
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
                logger.LogError(ex, "Error editing {AgencyId}. {UserEmail}.", id, userEmail);
                notifyService.Error("OOPS !!!..Error editing User. Try again.");
            }
            return RedirectToAction(nameof(Users), "EmpanelledAgencyUser", new { id = model.VendorId });
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> Delete(long userId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var model = await _context.ApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }

                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;

                var claimsPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Manage Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "EmpanelledAgency", "Empanelled Agencies") { Parent = claimsPage, };
                var detailsPage = new MvcBreadcrumbNode("Detail", "EmpanelledAgency", $"Agency Profile") { Parent = agencyPage, RouteValues = new { id = model.VendorId } };
                var editPage = new MvcBreadcrumbNode("Users", "EmpanelledAgencyUser", $"Manage Users") { Parent = detailsPage, RouteValues = new { id = model.VendorId } };
                var addPage = new MvcBreadcrumbNode("Delete", "EmpanelledAgencyUser", $"Delete user") { Parent = editPage };
                ViewData["BreadcrumbNode"] = addPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting {AgencyId}. {UserEmail}.", userId, userEmail);
                notifyService.Error("Error getting user. Try again");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string email, long vendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return this.RedirectToAction<DashboardController>(x => x.Index());
                }
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
                notifyService.Custom($"User <b>{model.Email}</b> deleted successfully", 3, "orange", "fas fa-user-minus");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting {AgencyUser}. {UserEmail}.", email, userEmail);
                notifyService.Error("Error deleting user. Try again");
            }
            return RedirectToAction(nameof(Users), "EmpanelledAgencyUser", new { id = vendorId });
        }
    }
}