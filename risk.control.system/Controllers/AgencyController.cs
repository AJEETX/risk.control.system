using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ITinyUrlService urlService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ISmsService smsService;
        private readonly IAgencyService agencyService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AgencyController> logger;
        private string portal_base_url = string.Empty;

        public AgencyController(ApplicationDbContext context,
            ITinyUrlService urlService,
            UserManager<VendorApplicationUser> userManager,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ICustomApiCLient customApiCLient,
            ISmsService SmsService,
            IAgencyService agencyService,
            IFeatureManager featureManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.urlService = urlService;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.customApiCLient = customApiCLient;
            smsService = SmsService;
            this.featureManager = featureManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            this.agencyService = agencyService;
            this.webHostEnvironment = webHostEnvironment;
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

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
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

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor vendor)
        {
            if (vendor is null || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Edit), "Agency");
            }
            try
            {
                if (vendor is null || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Edit), "Agency");
                }
                if (vendor == null || vendor.VendorId == 0)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();

                var edited = await agencyService.EditAgency(vendor, vendorDocument, currentUserEmail, portal_base_url);
                if (!edited)
                {
                    notifyService.Custom($"Agency {vendor.Email} not edited.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                notifyService.Custom($"Agency {vendor.Email} edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            return View();
        }

        [Breadcrumb("Add User")]
        public IActionResult CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = _context.Vendor.Include(v => v.Country).FirstOrDefault(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var roles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(role => role != AgencyRole.AGENCY_ADMIN)?.ToList();

                var model = new VendorApplicationUser { Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor, AgencyRole = roles };
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser user, string emailSuffix, string vendorId, string txn = "agency")
        {
            if (string.IsNullOrWhiteSpace(txn))
            {
                notifyService.Custom($"Error to create user.", 3, "red", "fas fa-user-plus");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (user is null || string.IsNullOrWhiteSpace(emailSuffix) || string.IsNullOrWhiteSpace(vendorId) || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                if (txn == "agency")
                {
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }
                else if (txn == "company")
                {
                    return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = vendorId });
                }
                else
                {
                    return RedirectToAction(nameof(VendorsController.Users), "Vendors", new { id = vendorId });
                }
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                notifyService.Custom($"Empty username.", 3, "red", "fas fa-building");
                if (txn == "agency")
                {
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }
                else if (txn == "company")
                {
                    return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = vendorId });
                }
                else
                {
                    return RedirectToAction(nameof(VendorsController.Users), "Vendors", new { id = vendorId });
                }
            }

            try
            {
                IFormFile profileFile = null;
                var files = Request.Form?.Files;
                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == user?.ProfileImage?.FileName && f.Name == user?.ProfileImage?.Name);
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }

                if (user.ProfileImage != null && user.ProfileImage.Length > 0 && !string.IsNullOrWhiteSpace(Path.GetFileName(user.ProfileImage.FileName)))
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    user.ProfilePictureExtension = fileExtension;
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                    user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    user.ProfilePictureUrl = "/agency/" + newFileName;

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                //DEMO
                user.Password = Applicationsettings.Password;

                user.PinCodeId = user.SelectedPincodeId;
                user.DistrictId = user.SelectedDistrictId;
                user.StateId = user.SelectedStateId;
                user.CountryId = user.SelectedCountryId;
                user.PhoneNumber = user.PhoneNumber.TrimStart('0');
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.Role = user.Role != null ? user.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                var pincode = _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(c => c.PinCodeId == user.PinCodeId);
                if (user.Role == AppRoles.AGENT)
                {
                    var userAddress = $"{user.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    user.AddressLatitude = coordinates.Latitude;
                    user.AddressLongitude = coordinates.Longitude;
                    user.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }

                IdentityResult result = await userManager.CreateAsync(user, user.Password);
                if (result.Succeeded)
                {
                    var roleResult = await userManager.AddToRolesAsync(user, new List<string> { user.UserRole.ToString() });
                    var roles = await userManager.GetRolesAsync(user);

                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            notifyService.Custom($"User {user.Email} created.", 3, "green", "fas fa-user-lock");
                            await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + portal_base_url);
                            if (txn == "agency")
                            {
                                return RedirectToAction(nameof(AgencyController.Users), "Agency");
                            }
                            else
                            {
                                return RedirectToAction(nameof(VendorsController.Users), "Vendors", new { id = vendorId });
                            }
                        }
                    }
                    else
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.Now);
                        var onboardAgent = createdUser.Role == AppConstant.AppRoles.AGENT && string.IsNullOrWhiteSpace(user.MobileUId);
                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            if (onboardAgent)
                            {
                                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);
                                string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                                var message = $"Dear {user.FirstName},\n" +
                                $"Click on link below to install the mobile app\n\n" +
                                $"{tinyUrl}\n\n" +
                                $"Thanks\n\n" +
                                $"{portal_base_url}";
                                await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, message, true);
                                notifyService.Custom($"Agent {user.Email} onboarding initiated.", 3, "green", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, "User created. \nEmail : " + user.Email + "\n" + portal_base_url);
                                notifyService.Custom($"User {user.Email} created.", 3, "green", "fas fa-user-check");
                            }
                        }

                        if (txn == "agency")
                        {
                            return RedirectToAction(nameof(AgencyController.Users), "Agency");
                        }
                        else if (txn == "company")
                        {
                            return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = vendorId });
                        }
                        else
                        {
                            return RedirectToAction(nameof(VendorsController.Users), "Vendors", new { id = vendorId });
                        }
                    }
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
                notifyService.Custom($"Error to create user.", 3, "red", "fas fa-user-plus");
                return View(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser applicationUser)
        {
            if (applicationUser is null || applicationUser.SelectedCountryId < 1 || applicationUser.SelectedStateId < 1 || applicationUser.SelectedDistrictId < 1 || applicationUser.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Agency");
            }

            try
            {
                if (id != applicationUser.Id.ToString() || applicationUser == null)
                {
                    notifyService.Error("Err !!! Bad Request");
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }
                IFormFile profileFile = null;
                var files = Request.Form?.Files;
                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == applicationUser?.ProfileImage?.FileName && f.Name == applicationUser?.ProfileImage?.Name);
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }

                if (profileFile != null && profileFile.Length > 0)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(profileFile.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                    profileFile.CopyTo(new FileStream(upload, FileMode.Create));
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfilePicture = dataStream.ToArray();
                    applicationUser.ProfilePictureUrl = "/agency/" + newFileName;
                    applicationUser.ProfilePictureExtension = fileExtension;
                }

                user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
                user.ProfilePictureExtension = applicationUser?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                user.FirstName = applicationUser?.FirstName;
                user.LastName = applicationUser?.LastName;
                if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                {
                    user.Password = applicationUser.Password;
                }
                user.PinCodeId = applicationUser.SelectedPincodeId;
                user.DistrictId = applicationUser.SelectedDistrictId;
                user.StateId = applicationUser.SelectedStateId;
                user.CountryId = applicationUser.SelectedCountryId;

                user.Addressline = applicationUser.Addressline;
                user.Active = applicationUser.Active;
                user.IsUpdated = true;
                user.Updated = DateTime.Now;
                user.Comments = applicationUser.Comments;
                user.PhoneNumber = applicationUser.PhoneNumber.TrimStart('0');
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.SecurityStamp = DateTime.Now.ToString();
                user.UserRole = applicationUser.UserRole;
                user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                var pincode = _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(c => c.PinCodeId == user.PinCodeId);
                if (user.Role == AppRoles.AGENT)
                {
                    var userAddress = $"{user.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    user.AddressLatitude = coordinates.Latitude;
                    user.AddressLongitude = coordinates.Longitude;
                    user.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }
                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                    await userManager.AddToRoleAsync(user, user.UserRole.ToString());
                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, "User edited. \nEmail : " + user.Email + "\n" + portal_base_url);
                            notifyService.Custom($"User {user.Email} edited.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.Now);
                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);
                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            if (onboardAgent)
                            {
                                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

                                string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                                var message = $"Dear {user.FirstName}\n" +
                                $"Click on link below to install the mobile app\n\n" +
                                $"{tinyUrl}\n\n" +
                                $"Thanks\n\n" +
                                $"{portal_base_url}";
                                await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, message, true);
                                notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, "User edited and unlocked. \nEmail : " + user.Email + "\n" + portal_base_url);
                                notifyService.Custom($"User {user.Email} edited.", 3, "orange", "fas fa-user-check");
                            }
                        }
                    }
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
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

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1)
                {
                    notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefault(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.VendorApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User {model.Email} deleted", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(AgencyController.Users), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Role", FromAction = "Users")]
        public async Task<IActionResult> UserRoles(string userId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var userRoles = new List<VendorUserRoleViewModel>();
            VendorApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Custom("OOPs !!!..User Not Found.", 3, "red", "fas fa-user-plus");
                return RedirectToAction(nameof(AgencyController.Users), "Agency");
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string userId, VendorUserRolesViewModel model)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Error("OOPs !!!..User Not Found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            var newRoles = model.VendorUserRoleViewModel.Where(x => x.Selected).Select(y => y.RoleName);
            result = await userManager.AddToRolesAsync(user, newRoles);

            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            await signInManager.RefreshSignInAsync(currentUser);

            var onboardAgent = newRoles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId) && user.Active;
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

            string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

            var message = $"Dear {user.FirstName}\n" +
            $"Click on link below to install the mobile app\n\n" +
            $"{tinyUrl}\n\n" +
            $"Thanks\n\n" +
            $"{portal_base_url}";
            if (onboardAgent)
            {
                var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, message);
                notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
            }
            else
            {
                notifyService.Custom($"User {user.Email} role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            }
            return RedirectToAction(nameof(AgencyController.Users), "Agency");
        }

        [Breadcrumb("Manage Service")]
        public IActionResult Service()
        {
            return View();
        }

        [Breadcrumb("Add Service")]
        public IActionResult CreateService()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                var vendor = _context.Vendor.Include(v => v.Country).FirstOrDefault(v => v.VendorId == vendorUser.VendorId);

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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictIds.Count <= 0))
            {
                notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(CreateService), "Agency");
            }

            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (vendorUser == null || !vendorUser.VendorId.HasValue)
                {
                    notifyService.Error("Vendor not found. Please check your login or vendor configuration.");
                    return RedirectToAction(nameof(CreateService), "Agency");
                }
                var isCountryValid = await _context.Country.AnyAsync(c => c.CountryId == service.SelectedCountryId);
                var isStateValid = await _context.State.AnyAsync(s => s.StateId == service.SelectedStateId);

                if (!isCountryValid || !isStateValid)
                {
                    notifyService.Error("Invalid country/state.");
                    return RedirectToAction(nameof(Service), "Agency");
                }

                var stateWideServices = _context.VendorInvestigationServiceType
                        .AsEnumerable() // Switch to client-side evaluation
                        .Where(v =>
                            v.VendorId == vendorUser.VendorId.Value &&
                            v.InsuranceType == service.InsuranceType &&
                            v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                            v.CountryId == service.SelectedCountryId &&
                            v.StateId == service.SelectedStateId)?
                        .ToList();
                bool isAllDistricts = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected
                // Handle state-wide service existence
                if (isAllDistricts)
                {
                    // Handle state-wide service creation
                    if (stateWideServices is not null && stateWideServices.Any(s => s.SelectedDistrictIds?.Contains(-1) == true))
                    {
                        var currentService = stateWideServices.FirstOrDefault(s => s.SelectedDistrictIds?.Contains(-1) == true);
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service [{ALL_DISTRICT}] already exists for the State!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(Service), "Agency");
                    }
                }
                else
                {
                    // Handle district-wide service creation
                    if (stateWideServices != null && stateWideServices.Any(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any()))
                    {
                        var currentService = stateWideServices.FirstOrDefault(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any());
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom("Service already exists for the District!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(Service), "Agency");
                    }
                }
                service.VendorId = vendorUser.VendorId.Value;
                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;
                service.IsUpdated = true;
                service.Updated = DateTime.Now;
                service.UpdatedBy = currentUserEmail;
                service.Created = DateTime.Now;

                _context.Add(service);
                await _context.SaveChangesAsync();
                if (isAllDistricts)
                {
                    notifyService.Custom($"Service [{ALL_DISTRICT}] added successfully.", 3, "orange", "fas fa-truck");
                }
                else
                {
                    notifyService.Custom("Service created successfully.", 3, "green", "fas fa-truck");
                }
                return RedirectToAction(nameof(AgencyController.Service), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Service), "Agency");
            }
        }

        [Breadcrumb("Edit Service", FromAction = "Service")]
        public IActionResult EditService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.VendorApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long vendorInvestigationServiceTypeId, VendorInvestigationServiceType service)
        {
            if (vendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictIds.Count <= 0))
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(EditService), "Agency", new { id = vendorInvestigationServiceTypeId });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(user => user.Email == currentUserEmail);
                var existingVendorServices = _context.VendorInvestigationServiceType
                       .AsNoTracking() // Switch to client-side evaluation
                       .Where(v =>
                           v.VendorId == vendorUser.VendorId.Value &&
                           v.InsuranceType == service.InsuranceType &&
                           v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                           v.CountryId == service.SelectedCountryId &&
                           v.StateId == service.SelectedStateId &&
                           v.VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId)?
                       .ToList();
                bool isAllDistricts = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected

                if (isAllDistricts)
                {
                    // Handle state-wide service creation
                    if (existingVendorServices is not null && existingVendorServices.Any(s => s.SelectedDistrictIds?.Contains(-1) == true))
                    {
                        var currentService = existingVendorServices.FirstOrDefault(s => s.SelectedDistrictIds?.Contains(-1) == true);
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service [{ALL_DISTRICT}] already exists for the State!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(Service), "Agency");
                    }
                }
                else
                {
                    // Handle state-wide service creation
                    if (existingVendorServices is not null && existingVendorServices.Any(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any()))
                    {
                        var currentService = existingVendorServices.FirstOrDefault(s => s.SelectedDistrictIds != null && s.SelectedDistrictIds.Intersect(service.SelectedDistrictIds ?? new List<long>()).Any());
                        currentService.IsUpdated = true;
                        _context.VendorInvestigationServiceType.Update(currentService);
                        await _context.SaveChangesAsync();
                        notifyService.Custom($"Service already exists for the District!", 3, "orange", "fas fa-truck");
                        return RedirectToAction(nameof(Service), "Agency");
                    }
                }
                service.CountryId = service.SelectedCountryId;
                service.StateId = service.SelectedStateId;

                service.Updated = DateTime.Now;
                service.UpdatedBy = currentUserEmail;
                service.IsUpdated = true;
                _context.Update(service);
                await _context.SaveChangesAsync();

                notifyService.Custom($"Service updated successfully.", 3, "orange", "fas fa-truck");
                return RedirectToAction(nameof(Service), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Service), "Agency");
            }
        }

        [Breadcrumb("Delete Service", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"NOT FOUND.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = _context.VendorApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
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
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error to delete service.", 3, "red", "fas fa-truck");
                return RedirectToAction("Service", "Agency");
            }
        }

        [Breadcrumb("Services", FromAction = "Service")]
        public async Task<IActionResult> ServiceDetail(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("NOT FOUND !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                    notifyService.Error("NOT FOUND !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agent Load", FromAction = "Users")]
        public IActionResult AgentLoad()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }
    }
}