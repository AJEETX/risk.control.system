using System.Net;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
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
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
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
            ILogger<AgencyUserProfileController> logger)
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
            UserList = new List<UsersViewModel>();
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.VendorApplicationUser
                    .Include(u => u.PinCode)
                    .Include(u => u.Country)
                    .Include(u => u.State)
                    .Include(u => u.District)
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                return View(vendorUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
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
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VendorApplicationUser model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    return View(model);
                }
                if (id != model.Id.ToString())
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var user = await userManager.FindByIdAsync(id);
                if (model?.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    if (model.ProfileImage.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                        return View(model);
                    }
                    var domain = model.Email.Split('@')[1];
                    domain = WebUtility.HtmlEncode(domain);
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, domain, "user");
                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();

                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);
                }

                if (user != null)
                {
                    user.Addressline = WebUtility.HtmlEncode(model?.Addressline) ?? user.Addressline;
                    user.ProfilePicture = model?.ProfilePicture ?? user.ProfilePicture;
                    user.ProfilePictureUrl = model?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.FirstName = WebUtility.HtmlEncode(model?.FirstName);
                    user.LastName = WebUtility.HtmlEncode(model?.LastName);
                    if (!string.IsNullOrWhiteSpace(model?.Password))
                    {
                        user.Password = model.Password;
                    }
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.EmailConfirmed = true;
                    user.CountryId = model.SelectedCountryId;
                    user.StateId = model.SelectedStateId;
                    user.PinCodeId = model.SelectedPincodeId;
                    user.DistrictId = model.SelectedDistrictId;
                    user.IsUpdated = true;
                    user.Updated = DateTime.Now;
                    user.Comments = WebUtility.HtmlEncode(model.Comments);
                    user.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        notifyService.Custom($"User profile edited successfully.", 3, "orange", "fas fa-user");

                        var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
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
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Change Password ")]
        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser != null)
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await userManager.GetUserAsync(User);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
                    var admin = await _context.ApplicationUser.Include(u => u.Country).FirstOrDefaultAsync(u => u.IsSuperAdmin);
                    var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;

                    if (user == null)
                    {
                        notifyService.Error("OOPS !!!..Contact Admin");
                        return RedirectToAction("/Account/Login");
                    }
                    model.CurrentPassword = WebUtility.HtmlEncode(model.CurrentPassword);
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
                logger.LogError(ex, "Error occurred.");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Password Change Succees")]
        public async Task<IActionResult> ChangePasswordConfirmation()
        {
            try
            {

                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
                if (vendorUser != null)
                {
                    notifyService.Custom($"Password edited successfully.", 3, "orange", "fas fa-user");
                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
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