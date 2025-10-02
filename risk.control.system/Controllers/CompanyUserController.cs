using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Users", FromAction = "Details", FromController = typeof(ClientCompanyController))]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class CompanyUserController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IPasswordHasher<ClientCompanyApplicationUser> passwordHasher;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<CompanyUserController> logger;
        private string portal_base_url = string.Empty;

        public CompanyUserController(UserManager<ClientCompanyApplicationUser> userManager,
            IPasswordHasher<ClientCompanyApplicationUser> passwordHasher,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ISmsService SmsService,
             IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            ILogger<CompanyUserController> logger,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.passwordHasher = passwordHasher;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            smsService = SmsService;
            this.httpContextAccessor = httpContextAccessor;
            this.featureManager = featureManager;
            this.logger = logger;
            this._context = context;
            UserList = new List<UsersViewModel>();
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index(long id)
        {
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == id);

            var model = new CompanyUsersViewModel
            {
                Company = company,
                Users = UserList
            };
            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;


            return View(model);
        }

        [Breadcrumb("Details")]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            var clientApplicationUser = await _context.ClientCompanyApplicationUser
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.ClientCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clientApplicationUser == null)
            {
                return NotFound();
            }

            return View(clientApplicationUser);
        }

        // GET: ClientCompanyApplicationUser/Create
        [Breadcrumb("Add New", FromAction = "Index")]
        public IActionResult Create(long id)
        {
            var company = _context.ClientCompany.Include(c => c.Country).FirstOrDefault(v => v.ClientCompanyId == id);
            var model = new ClientCompanyApplicationUser { Country = company.Country, CountryId = company.CountryId, ClientCompany = company };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var createPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Create", "CompanyUser", $"Add User") { Parent = createPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        // POST: ClientCompanyApplicationUser/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCompanyApplicationUser user, string emailSuffix)
        {
            var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                string newFileName = userFullEmail;
                string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
                newFileName += fileExtension;
                string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                using var dataStream = new MemoryStream();
                user.ProfileImage.CopyTo(dataStream);
                user.ProfilePicture = dataStream.ToArray();
                user.ProfilePictureUrl = "/company/" + newFileName;
                user.ProfilePictureExtension = fileExtension;
            }
            //DEMO
            user.Active = true;
            user.Password = Applicationsettings.Password;
            user.Email = userFullEmail;
            user.EmailConfirmed = true;
            user.UserName = userFullEmail;
            user.PhoneNumber = user.PhoneNumber.TrimStart('0');
            user.PinCodeId = user.SelectedPincodeId;
            user.DistrictId = user.SelectedDistrictId;
            user.StateId = user.SelectedStateId;
            user.CountryId = user.SelectedCountryId;

            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            user.Id = 0;
            user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
            IdentityResult result = await userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, user.UserRole.ToString());
                var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Company account created. \nDomain : " + user.Email + "\n" + portal_base_url);
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


        // GET: ClientCompanyApplicationUser/Edit/5
        [Breadcrumb("Edit ")]
        public async Task<IActionResult> Edit(long? userId)
        {
            if (userId == null || userId <= 0)
            {
                notifyService.Error("company not found");
                return NotFound();
            }

            var user = await _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).Include(c => c.Country).FirstOrDefaultAsync(v => v.Id == userId);
            if (user == null)
            {
                notifyService.Error("company not found");
                return NotFound();
            }

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = user.ClientCompany.ClientCompanyId } };
            var createPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage, RouteValues = new { id = user.ClientCompany.ClientCompanyId } };
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
        public async Task<IActionResult> Edit(long id, ClientCompanyApplicationUser applicationUser)
        {
            try
            {
                var user = await userManager.FindByIdAsync(id.ToString());
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    string newFileName = user.Email + Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(applicationUser.ProfileImage.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfileImage.CopyTo(dataStream);
                    applicationUser.ProfilePicture = dataStream.ToArray();
                    applicationUser.ProfilePictureUrl = "/company/" + newFileName;
                    applicationUser.ProfilePictureExtension = fileExtension;
                }

                if (user != null)
                {
                    user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
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
                    user.Comments = applicationUser.Comments;
                    user.UserRole = applicationUser.UserRole;
                    user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                        await userManager.AddToRoleAsync(user, user.UserRole.ToString());
                        notifyService.Custom($"Company user edited successfully.", 3, "orange", "fas fa-user-check");
                        var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Company account edited. \nDomain : " + user.Email + "\n" + portal_base_url);

                        return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = applicationUser.ClientCompanyId });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }


        // GET: VendorApplicationUsers/Delete/5
        [Breadcrumb("Delete")]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorApplicationUser == null)
            {
                return NotFound();
            }

            return View(vendorApplicationUser);
        }

        // POST: VendorApplicationUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Problem("Entity set 'ApplicationDbContext.VendorApplicationUser'  is null.");
            }
            var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.FindAsync(id);
            if (clientCompanyApplicationUser != null)
            {
                clientCompanyApplicationUser.Updated = DateTime.Now;
                clientCompanyApplicationUser.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompanyApplicationUser.Remove(clientCompanyApplicationUser);
            }

            await _context.SaveChangesAsync();
            notifyService.Error($"User deleted successfully.", 3);
            return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompanyId });
        }
    }
}