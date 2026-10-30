using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Company;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Breadcrumb(" Users", FromAction = "Details", FromController = typeof(ClientCompanyController))]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class CompanyUserController(UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorageService,
            IManageCompanyUserService manageCompanyUserService,
            IEncryptionService encryptionService,
            IPasswordHasher<ApplicationUser> passwordHasher,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ISmsService smsService,
             IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            ILogger<CompanyUserController> logger,
            ApplicationDbContext context) : Controller
    {
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly IManageCompanyUserService _manageCompanyUserService = manageCompanyUserService;
        private readonly IEncryptionService _encryptionService = encryptionService;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher = passwordHasher;
        private readonly INotyfService _notifyService = notifyService;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly ISmsService _smsService = smsService;
        private readonly ApplicationDbContext _context = context;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly ILogger<CompanyUserController> _logger = logger;

        public async Task<IActionResult> Index([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == id);

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };

            var encryptedId = _encryptionService.ProtectId(id);
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = encryptedId } };
            var editPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(company);
        }

        [Breadcrumb("Add New", FromAction = "Index")]
        public IActionResult Create([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            var company = _context.ClientCompany.Include(c => c.Country).FirstOrDefault(v => v.ClientCompanyId == id);
            var model = new ApplicationUser { Country = company!.Country, CountryId = company.CountryId, ClientCompany = company, ClientCompanyId = company.ClientCompanyId };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            var encryptedId = _encryptionService.ProtectId(id);

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = encryptedId } };
            var createPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage, RouteValues = new { id = encryptedId } };
            var editPage = new MvcBreadcrumbNode("Create", "CompanyUser", $"Add User") { Parent = createPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string emailSuffix)
        {
            emailSuffix = emailSuffix.Replace("\n", "").Replace("\r", "").Trim();
            var userFullEmail = user.Email!.Trim().ToLower() + "@" + emailSuffix;
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                var (fileName, relativePath) = await _fileStorageService.SaveAsync(user.ProfileImage, emailSuffix, "user");
                user.ProfilePictureUrl = relativePath;
            }
            //DEMO
            user.Active = true;
            user.Password = Applicationsettings.TestingData;
            user.Email = userFullEmail;
            user.EmailConfirmed = true;
            user.UserName = userFullEmail;

            user.PinCodeId = user.SelectedPincodeId;
            user.DistrictId = user.SelectedDistrictId;
            user.StateId = user.SelectedStateId;
            user.CountryId = user.SelectedCountryId;

            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            user.Id = 0;
            user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.Role.ToString()!);
            IdentityResult result = await _userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, user.Role.ToString()!);
                var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId)?.ISDCode;
                await _smsService.SendSmsAsync(isdCode + user.PhoneNumber, "Company account created. Domain : " + user.Email);
                _notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                var encryptedId = _encryptionService.ProtectId(user.ClientCompanyId!.Value);
                return RedirectToAction(nameof(CompanyUserController.Index), ControllerName<CompanyUserController>.Name, new { id = encryptedId });
            }
            else
            {
                _notifyService.Error("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            //GetCountryStateEdit(user);
            _notifyService.Error($"Err User create.", 3);
            return View(user);
        }

        [Breadcrumb("Edit ")]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id <= 0)
            {
                _notifyService.Error("Company not found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var user = await _manageCompanyUserService.GetUserForEditAsync(id);
            if (user == null)
            {
                _notifyService.Error("Company not found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var encryptedId = _encryptionService.ProtectId(user.ClientCompanyId!.Value);
            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = encryptedId } };
            var createPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage, RouteValues = new { id = encryptedId } };
            var editPage = new MvcBreadcrumbNode("Edit", "CompanyUser", $"Edit User") { Parent = createPage };
            ViewData["BreadcrumbNode"] = editPage;
            user.IsPasswordChangeRequired = !await _featureManager.IsEnabledAsync(nameof(FeatureFlags.FIRST_LOGIN_CONFIRMATION)) || !user.IsPasswordChangeRequired;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id, ApplicationUser applicationUser)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null) { _notifyService.Custom("User not found.", 3, "red", "fas fa-user-slash"); return RedirectToAction(nameof(Index), "Dashboard"); }
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    var emailSuffix = user!.Email!.Split('@').LastOrDefault();
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(user.ProfileImage!, emailSuffix!, "user");
                    user.ProfilePictureUrl = relativePath;
                }
                user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePictureExtension = applicationUser?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                user.FirstName = applicationUser?.FirstName!;
                user.LastName = applicationUser?.LastName!;
                user.Addressline = applicationUser!.Addressline;
                user.Active = applicationUser.Active;
                user.PhoneNumber = user.PhoneNumber!.TrimStart('0');
                user.CountryId = applicationUser.SelectedCountryId;
                user.StateId = applicationUser.SelectedStateId;
                user.DistrictId = applicationUser.SelectedDistrictId;
                user.PinCodeId = applicationUser.SelectedPincodeId;
                user.Updated = DateTime.Now;
                user.Role = applicationUser.Role;
                user.PhoneNumber = applicationUser.PhoneNumber;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.SecurityStamp = DateTime.Now.ToString();
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var roleResult = await _userManager.RemoveFromRolesAsync(user, roles);
                    await _userManager.AddToRoleAsync(user, user.Role.ToString()!);
                    _notifyService.Custom($"Company user edited successfully.", 3, "orange", "fas fa-user-check");
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                    await _smsService.DoSendSmsAsync(country!.Code, country.ISDCode + user.PhoneNumber, "Company account edited. \nDomain : " + user.Email + "\n" + _baseUrl);
                    var encryptedId = _encryptionService.ProtectId(applicationUser.ClientCompanyId!.Value);
                    return RedirectToAction(nameof(CompanyUserController.Index), ControllerName<CompanyUserController>.Name, new { id = encryptedId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred.");
            }
            _notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([ModelBinder(BinderType = typeof(EncryptedLongModelBinder))] long id)
        {
            if (id <= 0)
            {
                return Problem("Entity set 'ApplicationDbContext.ApplicationUser'  is null.");
            }
            var companyUser = await _context.ApplicationUser.FindAsync(id);
            if (companyUser != null)
            {
                companyUser.Updated = DateTime.UtcNow;
                companyUser.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ApplicationUser.Remove(companyUser);
            }

            await _context.SaveChangesAsync();
            _notifyService.Error($"User deleted successfully.", 3);
            var encryptedId = _encryptionService.ProtectId(companyUser!.ClientCompanyId!.Value);
            return RedirectToAction(nameof(CompanyUserController.Index), ControllerName<CompanyUserController>.Name, new { id = encryptedId });
        }
    }
}