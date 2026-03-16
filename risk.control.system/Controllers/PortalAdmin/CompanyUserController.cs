using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Breadcrumb(" Users", FromAction = "Details", FromController = typeof(ClientCompanyController))]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class CompanyUserController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IFileStorageService fileStorageService;
        private readonly IPasswordHasher<ApplicationUser> passwordHasher;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<CompanyUserController> logger;
        private string portal_base_url = string.Empty;

        public CompanyUserController(UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorageService,
            IPasswordHasher<ApplicationUser> passwordHasher,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService,
             IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            ILogger<CompanyUserController> logger,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.fileStorageService = fileStorageService;
            this.passwordHasher = passwordHasher;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            smsService = SmsService;
            this.httpContextAccessor = httpContextAccessor;
            this.featureManager = featureManager;
            this.logger = logger;
            this._context = context;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index(long id)
        {
            var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == id);

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(company);
        }

        [Breadcrumb("Add New", FromAction = "Index")]
        public IActionResult Create(long id)
        {
            var company = _context.ClientCompany.Include(c => c.Country).FirstOrDefault(v => v.ClientCompanyId == id);
            var model = new ApplicationUser { Country = company.Country, CountryId = company.CountryId, ClientCompany = company, ClientCompanyId = company.ClientCompanyId };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var createPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Create", "CompanyUser", $"Add User") { Parent = createPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string emailSuffix)
        {
            var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                var (fileName, relativePath) = await fileStorageService.SaveAsync(user.ProfileImage, emailSuffix, "user");
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
            user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.Role.ToString());
            IdentityResult result = await userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, user.Role.ToString());
                var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId)?.ISDCode;
                await smsService.SendSmsAsync(isdCode + user.PhoneNumber, "Company account created. Domain : " + user.Email);
                notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");

                return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = user.ClientCompanyId });
            }
            else
            {
                notifyService.Error("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            //GetCountryStateEdit(user);
            notifyService.Error($"Err User create.", 3);
            return View(user);
        }

        [Breadcrumb("Edit ")]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null || id <= 0)
            {
                notifyService.Error("Company not found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var user = await _context.ApplicationUser.Include(c => c.Country).FirstOrDefaultAsync(v => v.Id == id);
            if (user == null)
            {
                notifyService.Error("Company not found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = user.ClientCompanyId } };
            var createPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage, RouteValues = new { id = user.ClientCompanyId } };
            var editPage = new MvcBreadcrumbNode("Edit", "CompanyUser", $"Edit User") { Parent = createPage };
            ViewData["BreadcrumbNode"] = editPage;
            user.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(nameof(FeatureFlags.FIRST_LOGIN_CONFIRMATION)) ? !user.IsPasswordChangeRequired : true;
            return View(user);
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, ApplicationUser applicationUser)
        {
            try
            {
                var user = await userManager.FindByIdAsync(id.ToString());
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    var emailSuffix = user.Email.Split('@').LastOrDefault();
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(user.ProfileImage, emailSuffix, "user");
                    user.ProfilePictureUrl = relativePath;
                }

                if (user != null)
                {
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.ProfilePictureExtension = applicationUser?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                    user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                    user.FirstName = applicationUser?.FirstName;
                    user.LastName = applicationUser?.LastName;
                    if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                    {
                        user.Password = applicationUser.Password;
                    }
                    user.Addressline = applicationUser.Addressline;
                    user.Active = applicationUser.Active;
                    user.PhoneNumber = user.PhoneNumber.TrimStart('0');
                    user.CountryId = applicationUser.SelectedCountryId;
                    user.StateId = applicationUser.SelectedStateId;
                    user.DistrictId = applicationUser.SelectedDistrictId;
                    user.PinCodeId = applicationUser.SelectedPincodeId;

                    user.Updated = DateTime.Now;
                    user.Role = applicationUser.Role;
                    user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.Role.ToString());
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                        await userManager.AddToRoleAsync(user, user.Role.ToString());
                        notifyService.Custom($"Company user edited successfully.", 3, "orange", "fas fa-user-check");
                        var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Company account edited. \nDomain : " + user.Email + "\n" + portal_base_url);

                        return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = applicationUser.ClientCompanyId });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Problem("Entity set 'ApplicationDbContext.ApplicationUser'  is null.");
            }
            var clientCompanyApplicationUser = await _context.ApplicationUser.FindAsync(id);
            if (clientCompanyApplicationUser != null)
            {
                clientCompanyApplicationUser.Updated = DateTime.UtcNow;
                clientCompanyApplicationUser.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ApplicationUser.Remove(clientCompanyApplicationUser);
            }

            await _context.SaveChangesAsync();
            notifyService.Error($"User deleted successfully.", 3);
            return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompanyId });
        }
    }
}