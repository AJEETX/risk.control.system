using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("User Profile ")]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}, {AGENT.DISPLAY_NAME}")]
    public class AgencyUserProfileController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotificationService service;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private readonly ILogger<AgencyUserProfileController> logger;
        private readonly IWebHostEnvironment webHostEnvironment;
        private string portal_base_url = string.Empty;

        public AgencyUserProfileController(ApplicationDbContext context,
            IFileStorageService fileStorageService,
            UserManager<VendorApplicationUser> userManager,
            INotificationService service,
             IHttpContextAccessor httpContextAccessor,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService,
            ILogger<AgencyUserProfileController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.fileStorageService = fileStorageService;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.service = service;
            this.httpContextAccessor = httpContextAccessor;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            smsService = SmsService;
            this.logger = logger;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public IActionResult Index()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser
                    .Include(u => u.PinCode)
                    .Include(u => u.Country)
                    .Include(u => u.State)
                    .Include(u => u.District)
                    .FirstOrDefault(c => c.Email == userEmail);

                return View(vendorUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Profile")]
        public async Task<IActionResult> Edit(long? userId)
        {
            try
            {
                if (userId == null || _context.VendorApplicationUser == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorApplicationUser = await _context.VendorApplicationUser.Include(v => v.Vendor).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendorApplicationUser.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendorApplicationUser.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendorApplicationUser.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", vendorApplicationUser.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", vendorApplicationUser.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendorApplicationUser.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", vendorApplicationUser.PinCodeId);

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VendorApplicationUser applicationUser)
        {
            try
            {
                if (id != applicationUser.Id.ToString())
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var user = await userManager.FindByIdAsync(id);
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    var domain = applicationUser.Email.Split('@')[1];
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(applicationUser.ProfileImage, domain);
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfileImage.CopyTo(dataStream);
                    applicationUser.ProfilePicture = dataStream.ToArray();

                    applicationUser.ProfilePictureUrl = relativePath;
                    applicationUser.ProfilePictureExtension = Path.GetExtension(fileName);
                }

                if (user != null)
                {
                    user.Addressline = applicationUser?.Addressline ?? user.Addressline;
                    user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.FirstName = applicationUser?.FirstName;
                    user.LastName = applicationUser?.LastName;
                    if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                    {
                        user.Password = applicationUser.Password;
                    }
                    user.Email = applicationUser.Email;
                    user.UserName = applicationUser.Email;
                    user.EmailConfirmed = true;
                    user.CountryId = applicationUser.SelectedCountryId;
                    user.StateId = applicationUser.SelectedStateId;
                    user.PinCodeId = applicationUser.SelectedPincodeId;
                    user.DistrictId = applicationUser.SelectedDistrictId;
                    user.IsUpdated = true;
                    user.Updated = DateTime.Now;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber.TrimStart('0');
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        notifyService.Custom($"User profile edited successfully.", 3, "orange", "fas fa-user");

                        var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited. \n Email : " + user.Email + "\n" + portal_base_url);

                        return RedirectToAction(nameof(Index), "Dashboard");
                    }
                    Errors(result);
                }

                notifyService.Custom($"Error to create edit user.", 3, "red", "fas fa-user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Change Password ")]
        public IActionResult ChangePassword()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (vendorUser != null)
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(CancellationToken ct, ChangePasswordViewModel model)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                    var ipAddressWithoutPort = ipAddress?.Split(':')[0];
                    var user = await userManager.GetUserAsync(User);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
                    var admin = _context.ApplicationUser.Include(u => u.Country).FirstOrDefault(u => u.IsSuperAdmin);
                    var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                    //var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, "login-success", user.Email, isAuthenticated);

                    if (user == null)
                    {

                        notifyService.Error("OOPS !!!..Contact Admin");
                        return RedirectToAction("/Account/Login");
                    }

                    var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                    if (!result.Succeeded)
                    {
                        string failedMessage = $"Dear {admin.Email}\n";
                        failedMessage += $"User {user.Email} changed password. New password: {model.NewPassword}\n";
                        failedMessage += $"{BaseUrl}";
                        await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, failedMessage);
                        notifyService.Error("OOPS !!!..Contact Admin");
                        return RedirectToAction("/Account/Login");
                    }

                    await signInManager.RefreshSignInAsync(user);

                    string message = $"Dear {admin.Email}\n";
                    message += $"User {user.Email} changed password. New password: {model.NewPassword}\n";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, message);


                    message = string.Empty;
                    message = $"Dear {user.Email}\n";
                    message += $"Your changed password: {model.NewPassword}\n";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + user.PhoneNumber, message);

                    return View("ChangePasswordConfirmation");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Password Change Succees")]
        public IActionResult ChangePasswordConfirmation()
        {
            try
            {

                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (vendorUser != null)
                {
                    notifyService.Custom($"Password edited successfully.", 3, "orange", "fas fa-user");
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Error("Error to create Agency user!");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}