using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb("User Profile")]
    [Authorize(Roles = $"{COMPANY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CompanyUserProfileController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;

        public CompanyUserProfileController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
             IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            ISmsService SmsService)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
            smsService = SmsService;
            UserList = new List<UsersViewModel>();
        }

        public IActionResult Index()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var companyUser = _context.ClientCompanyApplicationUser
                    .Include(u => u.PinCode)
                    .Include(u => u.Country)
                    .Include(u => u.State)
                    .Include(u => u.District)
                    .FirstOrDefault(c => c.Email == userEmail);

                return View(companyUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit Profile")]
        public async Task<IActionResult> Edit(long? userId)
        {
            try
            {
                if (userId == null || _context.ClientCompanyApplicationUser == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == userId);
                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(clientCompanyApplicationUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ClientCompanyApplicationUser applicationUser)
        {
            try
            {
                if (id != applicationUser.Id.ToString() || applicationUser is null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var user = await userManager.FindByIdAsync(id);
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
                    applicationUser.ProfilePictureUrl = "/company/" + newFileName;
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfileImage.CopyTo(dataStream);
                    applicationUser.ProfilePicture = dataStream.ToArray();
                    applicationUser.ProfilePictureExtension = fileExtension;
                }

                if (user != null)
                {
                    user.Addressline = applicationUser?.Addressline ?? user.Addressline;
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.ProfilePictureExtension = applicationUser?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                    user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
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
                    user.DistrictId = applicationUser.SelectedDistrictId;
                    user.PinCodeId = applicationUser.SelectedPincodeId;
                    user.Updated = DateTime.Now;
                    user.IsUpdated = true;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        notifyService.Custom($"User profile edited successfully.", 3, "orange", "fas fa-user");
                        var isdCode = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId)?.ISDCode;
                        await smsService.DoSendSmsAsync(isdCode + user.PhoneNumber, "User edited . Email : " + user.Email);
                        return RedirectToAction(nameof(Index), "Dashboard");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [Breadcrumb("Change Password")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            try
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
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
                    var admin = _context.ApplicationUser.Include(c => c.Country).FirstOrDefault(u => u.IsSuperAdmin);
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
                        string failedMessage = $"Dear {admin.Email}";
                        failedMessage += $"                                       ";
                        failedMessage += $"                       ";
                        failedMessage += $"User {user.Email} failed changed password. New password: {model.NewPassword}";
                        failedMessage += $"                                       ";
                        failedMessage += $"Thanks                                         ";
                        failedMessage += $"                                       ";
                        failedMessage += $"                                       ";
                        failedMessage += $"{BaseUrl}";
                        await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, failedMessage);
                        notifyService.Error("OOPS !!!..Contact Admin");
                        return RedirectToAction("/Account/Login");
                    }

                    await signInManager.RefreshSignInAsync(user);

                    string message = $"Dear {admin.Email}";
                    message += $"                                       ";
                    message += $"                       ";
                    message += $"User {user.Email} changed password. New password: {model.NewPassword}";
                    message += $"                                       ";
                    message += $"Thanks                                         ";
                    message += $"                                       ";
                    message += $"                                       ";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, message);


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
                    await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + user.PhoneNumber, message);

                    return View("ChangePasswordConfirmation");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpGet]
        [Breadcrumb("Password Change Success")]
        public IActionResult ChangePasswordConfirmation()
        {
            notifyService.Custom($"Password edited successfully.", 3, "orange", "fas fa-user");
            return View();
        }
    }
}