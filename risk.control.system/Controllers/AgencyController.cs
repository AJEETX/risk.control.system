using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class AgencyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorServiceTypeManager vendorServiceTypeManager;
        private readonly IAgencyUserCreateEditService agencyUserCreateEditService;
        private readonly IAgencyCreateEditService agencyCreateEditService;
        private readonly INotyfService notifyService;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AgencyController> logger;
        private string portal_base_url = string.Empty;

        public AgencyController(ApplicationDbContext context,
            IVendorServiceTypeManager vendorServiceTypeManager,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            IAgencyCreateEditService agencyCreateEditService,
            INotyfService notifyService,
            IFeatureManager featureManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyController> logger)
        {
            _context = context;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.vendorServiceTypeManager = vendorServiceTypeManager;
            this.agencyUserCreateEditService = agencyUserCreateEditService;
            this.agencyCreateEditService = agencyCreateEditService;
            this.notifyService = notifyService;
            this.featureManager = featureManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Breadcrumb("Admin Settings ")]
        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [Breadcrumb("Agency Profile ", FromAction = "Index")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(m => m.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Error("Agency Not Found! Contact ");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Agency");
                notifyService.Error("OOPs !!!...Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Agency", FromAction = "Profile")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"Agency Not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (vendorUser.IsVendorAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await Load(model);
                    return View(model);
                }

                var userEmail = HttpContext.User?.Identity?.Name;

                var result = await agencyCreateEditService.EditAsync(userEmail, model, portal_base_url);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    notifyService.Error("Please fix validation errors");
                    await Load(model);
                    return View(model);
                }
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Editing Agency");
                notifyService.Error("OOPS !!!..Error Editing Agency. Try again.");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
        }
        private async Task Load(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            if (vendorUser.IsVendorAdmin)
            {
                model.SelectedByCompany = true;
            }
            else
            {
                model.SelectedByCompany = false;
            }
        }
        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            return View();
        }

        [Breadcrumb("Add User")]
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var roles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(role => role != AgencyRole.AGENCY_ADMIN)?.ToList();

                var model = new VendorApplicationUser
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor,
                    AgencyRole = roles
                };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user.");
                notifyService.Error("OOPS !!!..Error creating user. Try again.");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
        }

        private async Task LoadModel(VendorApplicationUser model)
        {
            var roles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(role => role != AgencyRole.AGENCY_ADMIN)?.ToList();
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.Vendor = vendor;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AgencyRole = roles;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser model, string emailSuffix, string vendorId)
        {
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
                logger.LogError(ex, "Error Creating User. Try again.");
                notifyService.Error("OOPS !!!..Error Creating User. Try again.");
            }
            return RedirectToAction(nameof(AgencyController.Users), "Agency");
        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                if (userId == null || userId <= 0)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }

                var user = await _context.VendorApplicationUser.Include(u => u.Country).Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Id == userId);
                if (user == null)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
                user.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !user.IsPasswordChangeRequired : true;

                return View(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating User.");
            }
            notifyService.Error("OOPs !!!.Error creating User. Try again");
            return RedirectToAction(nameof(AgencyController.CreateUser), "Agency");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser model)
        {
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
                logger.LogError(ex, "Error editing User.");
                notifyService.Error("OOPS !!!..Error editing User. Try again.");
            }
            return RedirectToAction(nameof(AgencyController.Users), "Agency");
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1)
                {
                    notifyService.Error("OOPS!!!.Invalid Data.Try Again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.User Not Found.Try Again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }

                var agencySubStatuses = new[] {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting user.");
                notifyService.Error("OOPS!!!..Error deleting user. Try again");
                return RedirectToAction(nameof(AgencyController.User), "Agency");
            }
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string email)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
                var model = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.VendorApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b> {model.Email}</b> deleted successfully", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(AgencyController.Users), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting user.");
                notifyService.Error("OOPS!!!..Error deleting user. Try again");
                return RedirectToAction(nameof(AgencyController.User), "Agency");
            }
        }

        [Breadcrumb("Manage Service")]
        public IActionResult Service()
        {
            return View();
        }

        [Breadcrumb("Add Service")]
        public async Task<IActionResult> CreateService()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (vendorUser is null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor is null)
                {
                    notifyService.Error("Agency Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
                ViewData["Currency"] = Extensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var model = new VendorInvestigationServiceType
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor
                };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating service");
                notifyService.Error("Error creating service.Try again.");
                return RedirectToAction(nameof(AgencyController.Service), "Agency");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service)
        {
            try
            {
                var email = HttpContext.User?.Identity?.Name;

                var result = await vendorServiceTypeManager.CreateAsync(service, email);

                if (!result.Success)
                {
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.IsAllDistricts ? "orange" : "green", "fas fa-truck");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating service");
                notifyService.Error("Error creating service. Try again.");
            }
            return RedirectToAction(nameof(Service), "Agency");
        }

        [Breadcrumb("Edit Service", FromAction = "Service")]
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.VendorApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.Vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var vendorInvestigationServiceType = _context.VendorInvestigationServiceType
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.InsuranceType == vendorInvestigationServiceType.InsuranceType), "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing service");
                notifyService.Custom($"Error editing service. Try again", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long vendorInvestigationServiceTypeId, VendorInvestigationServiceType service)
        {
            try
            {
                var email = HttpContext.User?.Identity?.Name;

                var result = await vendorServiceTypeManager.EditAsync(vendorInvestigationServiceTypeId, service, email);
                if (!result.Success)
                {
                    notifyService.Custom(result.Message, 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom(result.Message, 3, result.Success ? "green" : "orange", "fas fa-truck");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing service.");
                notifyService.Custom("Error editing service. Try again.", 3, "red", "fas fa-truck");
            }
            return RedirectToAction(nameof(Service), "Agency");
        }

        [Breadcrumb("Delete Service", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Invalid Data.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Service), "Agency");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.VendorApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.Vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.Country)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Error($"Service Not Found. Try again");
                    return RedirectToAction(nameof(Service), "Agency");
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting service.");
                notifyService.Error($"Error deleting service. Try again");
                return RedirectToAction(nameof(Service), "Agency");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id <= 0)
                {
                    notifyService.Custom($"Service Not Found.", 3, "red", "fas fa-truck");
                    return RedirectToAction("Service", "Agency");
                }
                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType != null)
                {
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = currentUserEmail;
                    _context.VendorInvestigationServiceType.Remove(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service deleted successfully.", 3, "red", "fas fa-truck");
                    return RedirectToAction("Service", "Agency");
                }
                notifyService.Error($"Err Service delete.", 3);
                return RedirectToAction("Service", "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting service.");
                notifyService.Error($"Error deleting service. Try again");
                return RedirectToAction(nameof(Service), "Agency");
            }
        }

        [Breadcrumb("Services", FromAction = "Service")]
        public async Task<IActionResult> ServiceDetail(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("Service Not Found.");
                    return RedirectToAction(nameof(Service));
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.Country)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
                if (vendorInvestigationServiceType == null)
                {
                    notifyService.Error("Service Not Found.");
                    return RedirectToAction(nameof(Service));
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                notifyService.Error("Service Not Found.");
                return RedirectToAction(nameof(Service));
            }
        }
    }
}