using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("User Profile")]
    public class CompanyUserProfileController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly INotificationService service;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;

        public CompanyUserProfileController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            INotificationService service,
             IHttpContextAccessor httpContextAccessor,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IToastNotification toastNotification)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.service = service;
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            this.toastNotification = toastNotification;
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
            catch (Exception)
            {
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

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.FindAsync(userId);
                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("USER NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var clientCompany = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId);

                if (clientCompany == null)
                {
                    notifyService.Error("COMPANY NOT FOUND");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                clientCompanyApplicationUser.ClientCompany = clientCompany;
                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompanyApplicationUser.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompanyApplicationUser.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompanyApplicationUser.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompanyApplicationUser.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompanyApplicationUser.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompanyApplicationUser.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompanyApplicationUser.PinCodeId);
                return View(clientCompanyApplicationUser);
            }
            catch (Exception)
            {
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
                    string fileExtension = Path.GetExtension(applicationUser.ProfileImage.FileName);
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
                }

                if (user != null)
                {
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.ProfilePicture = applicationUser?.ProfilePicture;
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
                    user.Updated = DateTime.UtcNow;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.UtcNow.ToString();
                    var result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        notifyService.Custom($"User profile edited successfully.", 3, "green", "fas fa-user");
                        var response = SmsService.SendSingleMessage(user.PhoneNumber, "User edited . Email : " + user.Email);
                        return RedirectToAction(nameof(Index), "Dashboard");
                    }
                }
            }
            catch (Exception)
            {
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
                if(string.IsNullOrEmpty(userEmail))
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
            catch (Exception)
            {
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
                        SMS.API.SendSingleMessage("+" + admin.PhoneNumber, failedMessage);
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
                    SMS.API.SendSingleMessage("+" + admin.PhoneNumber, message);


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
                    SMS.API.SendSingleMessage("+" + user.PhoneNumber, message);

                    return View("ChangePasswordConfirmation");
                }

                return View(model);
            }
            catch (Exception)
            {
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