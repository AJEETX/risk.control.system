using System.Net;
using System.Text.RegularExpressions;

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
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private const string vallidDomainExpression = "^[a-zA-Z0-9.-]+$";
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ITinyUrlService urlService;
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<ApplicationUser> appUserManager;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ICustomApiClient customApiCLient;
        private readonly ISmsService smsService;
        private readonly IAgencyService agencyService;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AgencyController> logger;
        private string portal_base_url = string.Empty;

        public AgencyController(ApplicationDbContext context,
            ITinyUrlService urlService,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> appUserManager,
            UserManager<VendorApplicationUser> userManager,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ICustomApiClient customApiCLient,
            ISmsService SmsService,
            IAgencyService agencyService,
            IFeatureManager featureManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AgencyController> logger)
        {
            _context = context;
            this.urlService = urlService;
            this.fileStorageService = fileStorageService;
            this.appUserManager = appUserManager;
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

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

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

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await Load(model);
                    return View(model);
                }
                if (model.Document is not null)
                {
                    if (model.Document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.Document), "File too large.");
                        await Load(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.Document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                        await Load(model);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.Document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid Document Image  content type.");
                        await Load(model);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.Document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                        await Load(model);
                        return View(model);
                    }
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var edited = await agencyService.EditAgency(model, currentUserEmail, portal_base_url);
                if (!edited)
                {
                    notifyService.Custom($"Agency <b>{model.Email}</b> not edited.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Error Editing Agency");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
        }
        private async Task Load(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            if (vendorUser.IsVendorAdmin)
            {
                model.SelectedByCompany = true;
            }
            else
            {
                model.SelectedByCompany = false;
            }
        }
        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            return View();
        }

        [Breadcrumb("Add User")]
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var roles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(role => role != AgencyRole.AGENCY_ADMIN)?.ToList();

                var model = new VendorApplicationUser
                {
                    Country = vendor.Country,
                    CountryId = vendor.CountryId,
                    Vendor = vendor,
                    AgencyRole = roles
                };
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Error creating user");
                return RedirectToAction(nameof(AgencyController.Profile), "Agency");
            }
        }

        private async Task LoadModel(VendorApplicationUser model)
        {
            var roles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(role => role != AgencyRole.AGENCY_ADMIN)?.ToList();
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.Vendor = vendor;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AgencyRole = roles;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser model, string emailSuffix, string vendorId, string txn = "agency")
        {
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(txn))
                {
                    notifyService.Error($"Correct the error(s)");
                    await LoadModel(model);
                    return View(model);
                }
                if (!Regex.IsMatch(emailSuffix, vallidDomainExpression))
                {
                    ModelState.AddModelError("", "Invalid domain address.");
                    await LoadModel(model);
                    return View(model);
                }
                emailSuffix = WebUtility.HtmlEncode(emailSuffix);
                var userFullEmail = model.Email.Trim().ToLower() + "@" + emailSuffix;
                var userExist = await appUserManager.Users.AnyAsync(u => u.Email == userFullEmail && !u.Deleted);
                if (userExist)
                {
                    notifyService.Custom($"User with email <b>{userFullEmail}</b> already exists.", 3, "red", "fas fa-user-times");
                    await LoadModel(model);
                    return View(model);
                }
                if (model.ProfileImage == null || model.ProfileImage.Length == 0)
                {
                    notifyService.Error("Invalid ProfileImage Image ");
                    await LoadModel(model);
                    return View(model);
                }

                if (model.ProfileImage.Length > MAX_FILE_SIZE)
                {
                    notifyService.Error($"Document image Size exceeds the max size: 5MB");
                    ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                    await LoadModel(model);
                    return View(model);
                }

                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext))
                {
                    notifyService.Error($"Invalid Document image type");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                    await LoadModel(model);
                    return View(model);
                }

                if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                {
                    notifyService.Error($"Invalid Document Image content type");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                    await LoadModel(model);
                    return View(model);
                }

                if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                {
                    notifyService.Error($"Invalid or corrupted Document Image ");
                    ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                    await LoadModel(model);
                    return View(model);
                }

                if (model.ProfileImage != null && model.ProfileImage.Length > 0 && !string.IsNullOrWhiteSpace(Path.GetFileName(model.ProfileImage.FileName)))
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, emailSuffix, "user");

                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);

                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();
                }

                //DEMO
                model.Password = Applicationsettings.TestingData;
                model.FirstName = WebUtility.HtmlEncode(model.FirstName);
                model.LastName = WebUtility.HtmlEncode(model.LastName);
                model.PinCodeId = model.SelectedPincodeId;
                model.DistrictId = model.SelectedDistrictId;
                model.StateId = model.SelectedStateId;
                model.CountryId = model.SelectedCountryId;

                model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                model.Comments = WebUtility.HtmlEncode(model.Comments);
                model.Addressline = WebUtility.HtmlEncode(model.Addressline);
                model.Email = userFullEmail;
                model.EmailConfirmed = true;
                model.UserName = userFullEmail;
                model.Updated = DateTime.Now;
                model.UpdatedBy = HttpContext.User?.Identity?.Name;
                model.Role = model.Role != null ? model.Role : (AppRoles)Enum.Parse(typeof(AppRoles), model.UserRole.ToString());
                model.IsVendorAdmin = model.UserRole == AgencyRole.AGENCY_ADMIN;
                var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.PinCodeId == model.PinCodeId);
                if (model.Role == AppRoles.AGENT)
                {
                    var userAddress = $"{model.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    model.AddressLatitude = coordinates.Latitude;
                    model.AddressLongitude = coordinates.Longitude;
                    model.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }

                IdentityResult result = await userManager.CreateAsync(model, model.Password);
                if (result.Succeeded)
                {
                    var roleResult = await userManager.AddToRolesAsync(model, new List<string> { model.UserRole.ToString() });
                    var roles = await userManager.GetRolesAsync(model);

                    if (!model.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(model.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            notifyService.Custom($"User {model.Email} created.", 3, "green", "fas fa-user-lock");
                            await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + portal_base_url);

                        }
                    }
                    else
                    {
                        var createdUser = await userManager.FindByEmailAsync(model.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.Now);
                        var onboardAgent = createdUser.Role == AppConstant.AppRoles.AGENT && string.IsNullOrWhiteSpace(model.MobileUId);
                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            if (onboardAgent)
                            {
                                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == model.VendorId);
                                string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                                var message = $"Dear {model.FirstName},\n" +
                                $"Click on link below to install the mobile app\n\n" +
                                $"{tinyUrl}\n\n" +
                                $"Thanks\n\n" +
                                $"{portal_base_url}";
                                await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + model.PhoneNumber, message, true);
                                notifyService.Custom($"Agent {model.Email} onboarding initiated.", 3, "green", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + model.PhoneNumber, "User created. \nEmail : " + model.Email + "\n" + portal_base_url);
                                notifyService.Custom($"User <b> {model.Email}</b> created.", 3, "green", "fas fa-user-check");
                            }
                        }
                    }
                }
                else
                {
                    notifyService.Error("OOPS !!!..Error Creating User, Contact Admin");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Error Creating User, Contact Admin");
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
            }
            notifyService.Error("OOPs !!!.Error creating User, Try again");
            return RedirectToAction(nameof(AgencyController.CreateUser), "Agency");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }
                if (model.ProfileImage is not null)
                {
                    if (model.ProfileImage.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.ProfileImage), "File too large.");
                        await LoadModel(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file type.");
                        await LoadModel(model);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.ProfileImage.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid Document Image  content type.");
                        await LoadModel(model);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.ProfileImage))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.ProfileImage), "Invalid file content.");
                        await LoadModel(model);
                        return View(model);
                    }
                }
                if (model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(CreateUser), "Agency");
                }

                if (id != model.Id.ToString() || model == null)
                {
                    notifyService.Error("Err !!! Bad Request");
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    notifyService.Custom($"OOPs !!!..User Not Found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }

                if (model?.ProfileImage != null && model?.ProfileImage.Length > 0)
                {
                    var domain = user.Email.Split('@').Last();
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, domain, "user");

                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);

                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();
                }

                user.ProfilePictureUrl = model?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePicture = model?.ProfilePicture ?? user.ProfilePicture;
                user.ProfilePictureExtension = model?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                user.FirstName = WebUtility.HtmlEncode(model?.FirstName);
                user.LastName = WebUtility.HtmlEncode(model?.LastName);
                if (!string.IsNullOrWhiteSpace(model?.Password))
                {
                    user.Password = model.Password;
                }
                user.PinCodeId = model.SelectedPincodeId;
                user.DistrictId = model.SelectedDistrictId;
                user.StateId = model.SelectedStateId;
                user.CountryId = model.SelectedCountryId;

                user.Addressline = WebUtility.HtmlEncode(model.Addressline);
                user.Active = model.Active;
                user.IsUpdated = true;
                user.Updated = DateTime.Now;
                user.Comments = WebUtility.HtmlEncode(model.Comments);
                user.PhoneNumber = model.PhoneNumber.TrimStart('0');
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.SecurityStamp = DateTime.Now.ToString();
                user.UserRole = model.UserRole;
                user.Role = model.Role != null ? model.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.PinCodeId == user.PinCodeId);
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
                            notifyService.Custom($"User <b>{user.Email}</b> edited successfully.", 3, "orange", "fas fa-user-lock");
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
                                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == user.VendorId);

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
                                notifyService.Custom($"User <b>{user.Email}</b> edited successfully.", 3, "orange", "fas fa-user-check");
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
            }
            notifyService.Error("OOPs !!!.Error editing User, Try again");
            return RedirectToAction(nameof(AgencyController.User), "Agency");
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1)
                {
                    notifyService.Error("OOPS!!!.Invalid Data.Try Again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.User Not Found.Try Again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
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
                notifyService.Error("OOPS!!!..Error deleting user. Try again");
                return RedirectToAction(nameof(AgencyController.User), "Agency");
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
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.VendorApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User <b> {model.Email}</b> deleted successfully", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(AgencyController.Users), "Agency");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Error deleting user. Try again");
                return RedirectToAction(nameof(AgencyController.User), "Agency");
            }
        }

        [Breadcrumb("Manage Service")]
        public IActionResult Service()
        {
            return View();
        }

        [Breadcrumb("Add Service")]
        public async Task<IActionResult> CreateService()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (vendorUser is null)
                {
                    notifyService.Error("User Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
                if (vendor is null)
                {
                    notifyService.Error("Agency Not Found!!!..Try again");
                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
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
                notifyService.Error("Error creating service!!!..Try again");
                return RedirectToAction(nameof(AgencyController.Service), "Agency");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType service)
        {
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    notifyService.Error("Please correct the errors");
                //    return View(service);
                //}
                if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictIds.Count <= 0))
                {
                    notifyService.Custom("OOPs !!!..Invalid Data.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(CreateService), "Agency");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                notifyService.Error("OOPs !!!..Error creating service. Try again");
                return RedirectToAction(nameof(CreateService), "Agency");
            }
        }

        [Breadcrumb("Edit Service", FromAction = "Service")]
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.VendorApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    notifyService.Error("Please correct the errors");
                //    return View(service);
                //}
                if (vendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId || service is null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || (service.SelectedDistrictIds.Count <= 0))
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(EditService), "Agency", new { id = vendorInvestigationServiceTypeId });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(user => user.Email == currentUserEmail);
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
                    notifyService.Custom($"Invalid Data.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Service), "Agency");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = await _context.VendorApplicationUser.Include(c => c.Vendor).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
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
                    notifyService.Error($"Service Not Found. Try again");
                    return RedirectToAction(nameof(Service), "Agency");
                }

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error($"Error deleting service. Try again");
                return RedirectToAction(nameof(Service), "Agency");
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
                notifyService.Error($"Error deleting service. Try again");
                return RedirectToAction(nameof(Service), "Agency");
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
    }
}