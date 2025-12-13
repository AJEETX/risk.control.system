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
    [Breadcrumb("Agencies")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorApplicationUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService fileStorageService;
        private readonly ILogger<VendorApplicationUsersController> logger;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly INotyfService notifyService;
        private readonly ISmsService smsService;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private string portal_base_url = string.Empty;

        public VendorApplicationUsersController(ApplicationDbContext context,
            IFileStorageService fileStorageService,
            ILogger<VendorApplicationUsersController> logger,
            UserManager<VendorApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            INotyfService notifyService,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ISmsService SmsService)
        {
            _context = context;
            this.fileStorageService = fileStorageService;
            this.logger = logger;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.notifyService = notifyService;
            smsService = SmsService;
            this.featureManager = featureManager;
            this.httpContextAccessor = httpContextAccessor;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.District).Include(v => v.PinCode).Include(v => v.State).Include(v => v.Vendor);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Details(long? id)
        {
            try
            {
                if (id == null || id == 0 || _context.VendorApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorApplicationUser = await _context.VendorApplicationUser
                    .Include(v => v.Country)
                    .Include(v => v.District)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.Vendor)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (vendorApplicationUser == null)
                {
                    notifyService.Error($"Error: User Not Found", 3);
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = id } };
                var usersPage = new MvcBreadcrumbNode("Index", "VendorApplicationUsers", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Details", "VendorApplicationUsers", $"User Details") { Parent = usersPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public async Task<IActionResult> Create(long id)
        {
            try
            {
                if (id == 0 || _context.VendorApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                var model = new VendorApplicationUser { Country = vendor.Country, Vendor = vendor };
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

                var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = id } };
                var usersPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Create", "VendorApplicationUsers", $"Add User") { Parent = usersPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorApplicationUser user, string emailSuffix)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (user == null)
                {
                    notifyService.Error("OOPs !!!..User not found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(emailSuffix))
                {
                    notifyService.Error("OOPs !!!..Email suffix not found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (user.ProfileImage != null && user.ProfileImage.Length > 0)
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(user.ProfileImage, emailSuffix, "user");
                    user.ProfilePictureUrl = relativePath;
                    user.ProfilePictureExtension = Path.GetExtension(fileName);

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                user.PhoneNumber = user.PhoneNumber.TrimStart('0');
                //DEMO
                user.Password = Applicationsettings.TestingData;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Updated = DateTime.Now;
                user.UpdatedBy = currentUserEmail;
                IdentityResult result = await userManager.CreateAsync(user, user.Password);

                if (result.Succeeded)
                {
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created and locked. \nEmail : " + user.Email + "\n" + portal_base_url);
                            notifyService.Custom($"User edited and locked.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {
                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \n\nEmail : " + user.Email);
                        notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = user.VendorId });
                }
                else
                {
                    notifyService.Error("Error to create user!");
                    foreach (IdentityError error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
                notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                return View(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        public async Task<IActionResult> Edit(long? userId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (userId == null || _context.VendorApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Id Not found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorApplicationUser = await _context.VendorApplicationUser.Include(v => v.Vendor).Include(v => v.Country)?.FirstOrDefaultAsync(v => v.Id == userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = vendorApplicationUser.Vendor.VendorId } };
                var usersPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = vendorApplicationUser.Vendor.VendorId } };
                var editPage = new MvcBreadcrumbNode("Edit", "VendorApplicationUsers", $"Edit User") { Parent = usersPage, RouteValues = new { id = userId } };
                ViewData["BreadcrumbNode"] = editPage;
                vendorApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !vendorApplicationUser.IsPasswordChangeRequired : true;

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VendorApplicationUser applicationUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var user = await userManager.FindByIdAsync(id);
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    var domain = user.Email.Split('@')[1];
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(user.ProfileImage, domain, "user");
                    user.ProfilePictureUrl = relativePath;
                    user.ProfilePictureExtension = Path.GetExtension(fileName);

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();
                }

                if (user != null)
                {
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
                    user.Country = applicationUser.Country;
                    user.CountryId = applicationUser.CountryId;
                    user.State = applicationUser.State;
                    user.StateId = applicationUser.StateId;
                    user.PinCode = applicationUser.PinCode;
                    user.PinCodeId = applicationUser.PinCodeId;
                    user.Updated = DateTime.Now;
                    user.IsUpdated = true;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber.TrimStart('0');
                    user.UpdatedBy = currentUserEmail;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                        if (!user.Active)
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited and locked. \nEmail : " + user.Email + "\n" + portal_base_url);
                                notifyService.Custom($"User edited and locked.", 3, "orange", "fas fa-user-lock");
                            }
                        }
                        else
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.Now);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited and unlocked. \n\nEmail : " + user.Email);
                                notifyService.Custom($"User edited.", 3, "green", "fas fa-user-check");
                            }
                        }
                        return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = applicationUser.VendorId });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

            notifyService.Error("Error !!. The user can't be edited!");
            return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = applicationUser.VendorId });
        }

        public async Task<IActionResult> UserRoles(string userId)
        {
            var userRoles = new List<VendorUserRoleViewModel>();
            VendorApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Error("user not found!");
                return NotFound();
            }
            foreach (var role in roleManager.Roles.Where(r =>
                r.Name.Contains(AppRoles.AGENCY_ADMIN.ToString()) ||
                r.Name.Contains(AppRoles.SUPERVISOR.ToString()) ||
                r.Name.Contains(AppRoles.AGENT.ToString())))
            {
                var userRoleViewModel = new VendorUserRoleViewModel
                {
                    RoleId = role.Id.ToString(),
                    RoleName = role?.Name
                };
                if (await userManager.IsInRoleAsync(user, role?.Name))
                {
                    userRoleViewModel.Selected = true;
                }
                else
                {
                    userRoleViewModel.Selected = false;
                }
                userRoles.Add(userRoleViewModel);
            }
            var model = new VendorUserRolesViewModel
            {
                UserId = userId,
                VendorId = user.VendorId.Value,
                UserName = user.UserName,
                VendorUserRoleViewModel = userRoles
            };
            var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = user.VendorId } };
            var usersPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = user.VendorId } };
            var editPage = new MvcBreadcrumbNode("UserRoles", "VendorApplicationUsers", $"Edit Role") { Parent = usersPage, RouteValues = new { id = user.Id } };
            ViewData["BreadcrumbNode"] = editPage;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string userId, VendorUserRolesViewModel model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(userId))
                {
                    notifyService.Error("OOPs !!!..User not found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                if (model == null)
                {
                    notifyService.Error("OOPs !!!..Model not found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                var roles = await userManager.GetRolesAsync(user);
                var result = await userManager.RemoveFromRolesAsync(user, roles);
                result = await userManager.AddToRolesAsync(user, model.VendorUserRoleViewModel.
                    Where(x => x.Selected).Select(y => y.RoleName));

                notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
                return RedirectToAction(nameof(VendorUserController.Index), "VendorUser", new { id = model.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || _context.VendorApplicationUser == null)
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

            var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            var usersPage = new MvcBreadcrumbNode("Index", "VendorApplicationUsers", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Delete", "VendorApplicationUsers", $"Delete User") { Parent = usersPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View(vendorApplicationUser);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(id);
                if (vendorApplicationUser == null)
                {
                    notifyService.Error($"Err User delete. Try again", 3);
                    return RedirectToAction(nameof(Index));
                }
                vendorApplicationUser.Updated = DateTime.Now;
                vendorApplicationUser.UpdatedBy = currentUserEmail;
                _context.VendorApplicationUser.Remove(vendorApplicationUser);
                notifyService.Error($"User deleted successfully.", 3);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}