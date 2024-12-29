using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;

using risk.control.system.Models;
using SmartBreadcrumbs.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using risk.control.system.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.FeatureManagement;
using Microsoft.AspNetCore.Http;
using risk.control.system.AppConstant;
using Microsoft.AspNetCore.Authorization;
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
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotificationService service;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgencyUserProfileController(ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager,
            INotificationService service,
             IHttpContextAccessor httpContextAccessor,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.service = service;
            this.httpContextAccessor = httpContextAccessor;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            smsService = SmsService;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
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
                    return NotFound();
                }

                var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return NotFound();
                }
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorApplicationUser.VendorId);

                if (vendor == null)
                {
                    notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                    return NotFound();
                }
                vendorApplicationUser.Vendor = vendor;

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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
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
                    string newFileName = applicationUser.Email + Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName( applicationUser.ProfileImage.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                    applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    applicationUser.ProfilePictureUrl = "/agency/" + newFileName;
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfileImage.CopyTo(dataStream);
                    applicationUser.ProfilePicture = dataStream.ToArray();
                }

                if (user != null)
                {
                    user.Addressline = applicationUser?.Addressline ?? user.Addressline;
                    user.PinCodeId = applicationUser?.SelectedPincodeId ?? user.PinCodeId;
                    user.StateId = applicationUser?.SelectedStateId ?? user.StateId;
                    user.DistrictId = applicationUser?.SelectedDistrictId ?? user.DistrictId;
                    user.CountryId = applicationUser?.SelectedCountryId ?? user.CountryId;
                    user.ProfileImage = applicationUser?.ProfileImage ?? user.ProfileImage;
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                    user.FirstName = applicationUser?.FirstName;
                    user.LastName = applicationUser?.LastName;
                    if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                    {
                        user.Password = applicationUser.Password;
                    }
                    user.Email = applicationUser.Email;
                    user.UserName = applicationUser.Email;
                    user.EmailConfirmed = true;
                    user.Country = applicationUser.Country;
                    user.CountryId = applicationUser.CountryId;
                    user.State = applicationUser.State;
                    user.StateId = applicationUser.StateId;
                    user.PinCode = applicationUser.PinCode;
                    user.PinCodeId = applicationUser.PinCodeId;
                    user.Updated = DateTime.Now;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        notifyService.Custom($"User profile edited successfully.", 3, "green", "fas fa-user");

                        await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user edited. Email : " + user.Email);

                        return RedirectToAction(nameof(Index), "Dashboard");
                    }
                    Errors(result);
                }

                notifyService.Custom($"Error to create edit user.", 3, "red", "fas fa-user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
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
                    var admin = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin);
                            var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                    var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, "login-success", user.Email, isAuthenticated);

                    if (user == null)
                    {

                        notifyService.Error("OOPS !!!..Contact Admin");
                        return RedirectToAction("/Account/Login");
                    }

                    var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                    if (!result.Succeeded)
                    {
                        string failedMessage = $"Dear {admin.Email}";
                        failedMessage += $"                                       ";
                        failedMessage += $"                       ";
                        failedMessage += $"User {user.Email} failed changed password from IP address {ipApiResponse.query}. New password: {model.NewPassword}";
                        failedMessage += $"                                       ";
                        failedMessage += $"Thanks                                         ";
                        failedMessage += $"                                       ";
                        failedMessage += $"                                       ";
                        failedMessage += $"{BaseUrl}";
                        await smsService.DoSendSmsAsync("+" + admin.PhoneNumber, failedMessage);
                        notifyService.Error("OOPS !!!..Contact Admin");
                        return RedirectToAction("/Account/Login");
                    }

                    await signInManager.RefreshSignInAsync(user);

                    string message = $"Dear {admin.Email}";
                    message += $"                                       ";
                    message += $"                       ";
                    message += $"User {user.Email} changed password from IP address {ipApiResponse.query}. New password: {model.NewPassword}";
                    message += $"                                       ";
                    message += $"Thanks                                         ";
                    message += $"                                       ";
                    message += $"                                       ";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync("+" + admin.PhoneNumber, message);


                    message = string.Empty;
                    message = $"Dear {user.Email}";
                    message += $"                                       ";
                    message += $"                       ";
                    message += $"Your changed password: {model.NewPassword}";
                    message += $"                                       ";
                    message += $"Thanks                                         ";
                    message += $"                                       ";
                    message += $"                                       ";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync("+" + user.PhoneNumber, message);

                    return View("ChangePasswordConfirmation");
                }

                return View(model);
            }
            catch (Exception ex)
            {
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