using System.Net;
using System.Text.RegularExpressions;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class VendorsController : Controller
    {
        private const string vendorMapSize = "800x800";
        private const string vallidDomainExpression = "^[a-zA-Z0-9.-]+$";
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> appUserManager;
        private readonly IAgencyService agencyService;
        private readonly IFileStorageService fileStorageService;
        private readonly ITinyUrlService urlService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly INotyfService notifyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ISmsService smsService;
        private readonly IInvestigationService service;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<VendorsController> logger;
        private string portal_base_url = string.Empty;

        public VendorsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> appUserManager,
            IAgencyService agencyService,
            IFileStorageService fileStorageService,
            ITinyUrlService urlService,
            UserManager<VendorApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            INotyfService notifyService,
            ICustomApiCLient customApiCLient,
            ISmsService SmsService,
            IInvestigationService service,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<VendorsController> logger)
        {
            _context = context;
            this.appUserManager = appUserManager;
            this.agencyService = agencyService;
            this.fileStorageService = fileStorageService;
            this.urlService = urlService;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.notifyService = notifyService;
            this.customApiCLient = customApiCLient;
            smsService = SmsService;
            this.service = service;
            this.featureManager = featureManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Index()
        {
            return RedirectToAction("EmpanelledVendors");
        }

        [Breadcrumb("Agencies", FromAction = "Index")]
        public IActionResult Agencies()
        {
            return View();
        }
        [Breadcrumb("Empanelled Agencies", FromAction = "Index")]
        public IActionResult EmpanelledVendors()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpanelledVendors(List<string> vendors)
        {
            try
            {
                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Not Agency Found");
                    return RedirectToAction("EmpanelledVendors", "Vendors");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found. Try again.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = await _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found. Try again.");
                    return RedirectToAction("EmpanelledVendors", "Vendors");
                }
                var empanelledVendors2Depanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));

                foreach (var empanelledVendor2Depanel in empanelledVendors2Depanel)
                {
                    var empanelled = company.EmpanelledVendors.FirstOrDefault(v => v.VendorId == empanelledVendor2Depanel.VendorId);
                    company.EmpanelledVendors.Remove(empanelled);
                }
                company.Updated = DateTime.Now;
                company.UpdatedBy = currentUserEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();
                notifyService.Custom($"Agency(s) De-panelled successfully.", 3, "orange", "far fa-hand-pointer");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Error occurred. Try again.");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }
        }

        [Breadcrumb("Available Agencies", FromAction = "Index")]
        public IActionResult AvailableVendors()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvailableVendors(List<string> vendors)
        {
            try
            {
                if (!ModelState.IsValid || vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("No agency selected !!!");
                    return RedirectToAction(nameof(AvailableVendors), "Company");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction("AvailableVendors", "Vendors");
                }
                var company = await _context.ClientCompany.Include(c => c.EmpanelledVendors)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction("AvailableVendors", "Vendors");
                }
                var vendors2Empanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                company.EmpanelledVendors.AddRange(vendors2Empanel.ToList());

                company.Updated = DateTime.Now;
                company.UpdatedBy = currentUserEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                notifyService.Custom($"Agency(s) empanelled successfully", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction("AvailableVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Error occurred. Try again.");
                return RedirectToAction("AvailableVendors", "Vendors");
            }

        }
        public async Task<JsonResult> PostRating(int rating, long mid)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var existingRating = await _context.Ratings.FirstOrDefaultAsync(r => r.VendorId == mid && r.UserEmail == currentUserEmail);
            if (existingRating != null)
            {
                existingRating.Rate = rating;
                _context.Ratings.Update(existingRating);
                _context.SaveChanges();
                return Json("You rated again " + rating.ToString() + " star(s)");
            }

            var rt = new AgencyRating();
            string ip = "123";
            rt.Rate = rating;
            rt.IpAddress = ip;
            rt.VendorId = mid;
            rt.UserEmail = currentUserEmail;
            _context.Ratings.Add(rt);
            _context.SaveChanges();
            return Json("You rated this " + rating.ToString() + " star(s)");
        }

        public async Task<JsonResult> PostDetailRating(int rating, long vendorId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Json(new { success = false, message = "You must be logged in to rate." });
            }

            var existingRating = await _context.Ratings.FirstOrDefaultAsync(r => r.VendorId == vendorId && r.UserEmail == currentUserEmail);

            if (existingRating != null)
            {
                existingRating.Rate = rating;
                _context.Ratings.Update(existingRating);
            }
            else
            {
                var newRating = new AgencyRating
                {
                    VendorId = vendorId,
                    Rate = rating,
                    UserEmail = currentUserEmail,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                };
                _context.Ratings.Add(newRating);
            }

            _context.SaveChanges();

            // Calculate new average rating
            var ratings = _context.Ratings.Where(r => r.VendorId == vendorId);
            double avgRating = ratings.Any() ? ratings.Average(r => r.Rate) : 0;

            return Json(new { success = true, message = $"You rated {rating} star(s)", avgRating = avgRating });
        }

        [Breadcrumb("Agency Profile", FromAction = "AvailableVendors")]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                if (id <= 0)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var vendorAllCasesCount = await _context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted &&
                          (c.SubStatus == approvedStatus ||
                          c.SubStatus == rejectedStatus));

                var vendorUserCount = await _context.VendorApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);

                // HACKY
                var currentCases = service.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
                vendor.SelectedCountryId = vendorUserCount;
                vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
                vendor.SelectedDistrictId = vendorAllCasesCount;

                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(" Manage Users", FromAction = "Details")]
        public IActionResult Users(string id)
        {
            ViewData["vendorId"] = id;

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View();
        }

        [Breadcrumb(" Add User", FromAction = "Users")]
        public async Task<IActionResult> CreateUser(long id)
        {
            if (id <= 0)
            {
                notifyService.Custom($"OOPs !!!..Error creating user.", 3, "red", "fa fa-user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var allRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>()?.ToList();
            AgencyRole? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentVendorUserCount = await _context.VendorApplicationUser.CountAsync(v => v.VendorId == id);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AgencyRole.AGENCY_ADMIN;
                status = true;
                allRoles = allRoles.Where(r => r == AgencyRole.AGENCY_ADMIN).ToList();
            }
            else
            {
                allRoles = allRoles.Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            }
            var model = new VendorApplicationUser
            {
                Active = status,
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                AgencyRole = allRoles,
                UserRole = role
            };

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("CreateUser", "Vendors", $"Add User") { Parent = usersPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }
        private async Task LoadModel(VendorApplicationUser model)
        {
            var allRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>()?.ToList();
            AgencyRole? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

            var currentVendorUserCount = await _context.VendorApplicationUser.CountAsync(v => v.VendorId == model.VendorId);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AgencyRole.AGENCY_ADMIN;
                status = true;
                allRoles = allRoles.Where(r => r == AgencyRole.AGENCY_ADMIN).ToList();
            }
            else
            {
                allRoles = allRoles.Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            }
            model.Active = status;
            model.Vendor = vendor;
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AgencyRole = allRoles;
            model.UserRole = role;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser model, string emailSuffix)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    await LoadModel(model);
                    return View(model);
                }
                if (!Regex.IsMatch(emailSuffix, vallidDomainExpression))
                {
                    ModelState.AddModelError("", "Invalid domain address.");
                    await LoadModel(model);
                    return View(model);
                }
                emailSuffix = WebUtility.HtmlEncode(emailSuffix.Trim().ToLower());
                model.Email = WebUtility.HtmlEncode(model.Email.Trim().ToLower());

                var userFullEmail = model.Email.Trim().ToLower() + "@" + emailSuffix;
                var userExist = await appUserManager.Users.AnyAsync(u => u.Email == userFullEmail && !u.Deleted);
                if (userExist)
                {
                    notifyService.Error($"User with email {userFullEmail} already exists.");
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

                if (string.IsNullOrEmpty(emailSuffix) || model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(CreateUser), "Vendors", new { d = model.VendorId });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
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
                model.Email = userFullEmail;
                model.EmailConfirmed = true;
                model.UserName = userFullEmail;
                model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
                model.PinCodeId = model.SelectedPincodeId;
                model.DistrictId = model.SelectedDistrictId;
                model.StateId = model.SelectedStateId;
                model.CountryId = model.SelectedCountryId;
                model.Addressline = WebUtility.HtmlEncode(model.Addressline);

                model.Role = (AppRoles)Enum.Parse(typeof(AppRoles), model.UserRole.ToString());
                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.IsVendorAdmin = model.UserRole == AgencyRole.AGENCY_ADMIN;
                if (model.Role == AppRoles.AGENT)
                {
                    var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.PinCodeId == model.PinCodeId);
                    var userAddress = $"{model.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";
                    var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(userAddress);
                    var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
                    model.AddressLatitude = coordinates.Latitude;
                    model.AddressLongitude = coordinates.Longitude;
                    model.AddressMapLocation = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                }
                model.Id = 0; // HACK, NOT SURE WHY ID is already SET
                IdentityResult result = await userManager.CreateAsync(model, model.Password);

                if (result.Succeeded)
                {
                    var roleResult = await userManager.AddToRolesAsync(model, new List<string> { model.UserRole.ToString() });
                    var roles = await userManager.GetRolesAsync(model);
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.CountryId);
                    if (!model.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(model.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + portal_base_url);
                            notifyService.Custom($"User <b>{model.Email}<b> created successfully", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {

                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + portal_base_url);

                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(model.MobileUId);

                        if (onboardAgent)
                        {
                            var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

                            string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                            var message = $"Dear {model.FirstName}\n";
                            message += $"Click on link below to install the mobile app\n\n";
                            message += $"{tinyUrl}\n\n";
                            message += $"Thanks\n\n";
                            message += $"{portal_base_url}";

                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, message, true);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, "Agency user created. \nEmail : " + model.Email + "\n" + portal_base_url);
                        }
                        notifyService.Custom($"User <b>{model.Email}<b> created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(Users), "Vendors", new { id = model.VendorId });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
            }
            notifyService.Error("OOPS !!!..Error Creating User, Try again");
            return RedirectToAction(nameof(CreateUser), "Vendors", new { d = model.VendorId });
        }

        [Breadcrumb(" Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                if (userId == null || userId <= 0)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorApplicationUser = _context.VendorApplicationUser
                    .Include(v => v.Country)?
                    .Include(v => v.Vendor)?
                    .FirstOrDefault(v => v.Id == userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = vendorApplicationUser.Vendor.VendorId } };
                var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = vendorApplicationUser.Vendor.VendorId } };
                var editPage = new MvcBreadcrumbNode("EditUser", "Vendors", $"Edit User") { Parent = usersPage };
                ViewData["BreadcrumbNode"] = editPage;

                vendorApplicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !vendorApplicationUser.IsPasswordChangeRequired : true;

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser model, string editby)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Custom($"Correct the error(s)", 3, "red", "fas fa-building");
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
                    return RedirectToAction(nameof(EditUser), "Vendors", new { userid = id });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (model?.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var domain = model.Email.Split('@')[1];
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(model.ProfileImage, domain, "user");
                    using var dataStream = new MemoryStream();
                    model.ProfileImage.CopyTo(dataStream);
                    model.ProfilePicture = dataStream.ToArray();

                    model.ProfilePictureUrl = relativePath;
                    model.ProfilePictureExtension = Path.GetExtension(fileName);
                }
                user.ProfilePicture = model?.ProfilePicture ?? user.ProfilePicture;
                user.ProfilePictureUrl = model?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePictureExtension = model?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                user.PhoneNumber = model?.PhoneNumber ?? user.PhoneNumber;
                user.PhoneNumber = WebUtility.HtmlEncode(user.PhoneNumber.TrimStart('0'));
                user.FirstName = WebUtility.HtmlEncode(model?.FirstName);
                user.LastName = WebUtility.HtmlEncode(model?.LastName);
                if (!string.IsNullOrWhiteSpace(model?.Password))
                {
                    user.Password = model.Password;
                }
                user.Addressline = WebUtility.HtmlEncode(model.Addressline);
                user.Active = model.Active;

                user.PinCodeId = model.SelectedPincodeId;
                user.DistrictId = model.SelectedDistrictId;
                user.StateId = model.SelectedStateId;
                user.CountryId = model.SelectedCountryId;

                user.IsUpdated = true;
                user.Updated = DateTime.Now;
                user.Comments = WebUtility.HtmlEncode(model.Comments);
                user.UpdatedBy = currentUserEmail;
                user.SecurityStamp = DateTime.Now.ToString();
                user.UserRole = model.UserRole;
                user.IsVendorAdmin = user.UserRole == null;
                user.Role = model.Role != null ? model.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());

                if (user.Role == AppRoles.AGENT)
                {
                    var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.PinCodeId == user.PinCodeId);
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
                    var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited and locked. \nEmail : " + user.Email + "\n" + portal_base_url);
                            notifyService.Custom($"User <b>{user.Email}<b> edited and locked.", 3, "orange", "fas fa-user-lock");
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
                                var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == user.VendorId);
                                string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                                var message = $"Dear {user.FirstName}\n";
                                message += $"Click on link below to install the mobile app\n\n";
                                message += $"{tinyUrl}\n\n";
                                message += $"Thanks\n\n";
                                message += $"{portal_base_url}";
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, message, true);
                                notifyService.Custom($"Agent onboarding initiated.", 3, "orange", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user edited.\n Email : " + user.Email + "\n" + portal_base_url);
                                notifyService.Custom($"User <b>{user.Email}<b> edited successfully.", 3, "orange", "fas fa-user-check");
                            }
                        }
                    }
                }
                else
                {
                    notifyService.Error("OOPS !!!..Error Editing User, Try again");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Error Editing User, Try again");
            }
            if (editby == "company")
            {
                return RedirectToAction(nameof(Users), "Vendors", new { id = model.VendorId });
            }
            else if (editby == "empanelled")
            {
                return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = model.VendorId });
            }
            else
            {
                return RedirectToAction(nameof(AgencyController.Users), "Agency");
            }
        }

        [Breadcrumb(title: " Delete", FromAction = "Users")]
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR};

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;

                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = model.VendorId } };
                var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = model.VendorId } };
                var editPage = new MvcBreadcrumbNode("DeleteUser", "Vendors", $"Delete User") { Parent = usersPage };
                ViewData["BreadcrumbNode"] = editPage;

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
        public async Task<IActionResult> DeleteUser(string email, long vendorId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Email == email);
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
                notifyService.Custom($"User <b>{model.Email}</b> Deleted successfully", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(VendorsController.Users), "Vendors", new { id = vendorId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Manage Service", FromAction = "Details")]
        public IActionResult Service(string id)
        {
            if (id == null || string.IsNullOrWhiteSpace(id))
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            ViewData["vendorId"] = id;

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var servicesPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manager Service") { Parent = agencyPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = servicesPage;

            return View();
        }

        [Breadcrumb(" Add Agency")]
        public async Task<IActionResult> Create()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = await _context.ClientCompanyApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var vendor = new Vendor { CountryId = companyUser.ClientCompany.CountryId, Country = companyUser.ClientCompany.Country, SelectedCountryId = companyUser.ClientCompany.CountryId.Value };
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor model, string domainAddress)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }
                if (!Regex.IsMatch(domainAddress, vallidDomainExpression))
                {
                    ModelState.AddModelError("", "Invalid domain address.");
                    await LoadModel(model);
                    return View(model);
                }

                if (model.Document == null || model.Document.Length == 0)
                {
                    notifyService.Error("Invalid Document Image ");
                    await LoadModel(model);
                    return View(model);
                }
                if (model.Document.Length > MAX_FILE_SIZE)
                {
                    notifyService.Error($"Document image Size exceeds the max size: 5MB");
                    ModelState.AddModelError(nameof(model.Document), "File too large.");
                    await LoadModel(model);
                    return View(model);
                }

                var ext = Path.GetExtension(model.Document.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext))
                {
                    notifyService.Error($"Invalid Document image type");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                    await LoadModel(model);
                    return View(model);
                }

                if (!AllowedMime.Contains(model.Document.ContentType))
                {
                    notifyService.Error($"Invalid Document Image content type");
                    ModelState.AddModelError(nameof(model.Document), "Invalid Document Image  content type.");
                    await LoadModel(model);
                    return View(model);
                }

                if (!ImageSignatureValidator.HasValidSignature(model.Document))
                {
                    notifyService.Error($"Invalid or corrupted Document Image ");
                    ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                    await LoadModel(model);
                    return View(model);
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                domainAddress = WebUtility.HtmlEncode(domainAddress.Trim().ToLower());
                var created = await agencyService.CreateAgency(model, currentUserEmail, domainAddress, portal_base_url);

                notifyService.Custom($"Agency <b>{model.Email}</b>  created successfully.", 3, "green", "fas fa-building");
                return RedirectToAction(nameof(CompanyController.AvailableVendors), "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        private async Task LoadModel(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        [Breadcrumb(" Edit Agency", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (1 > id)
                {
                    notifyService.Error("OOPS !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var isSuperAdmin = await _context.ApplicationUser.AnyAsync(u => u.Email.ToLower() == currentUserEmail.ToLower() && u.IsSuperAdmin);
                vendor.SelectedByCompany = isSuperAdmin;
                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit Agency") { Parent = agencyPage };
                ViewData["BreadcrumbNode"] = editPage;

                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    notifyService.Error("Please correct the errors");
                    await LoadModel(model);
                    return View(model);
                }
                if (model.Document is not null)
                {
                    if (model.Document.Length > MAX_FILE_SIZE)
                    {
                        notifyService.Error($"Document image Size exceeds the max size: 5MB");
                        ModelState.AddModelError(nameof(model.Document), "File too large.");
                        await LoadModel(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.Document.FileName).ToLowerInvariant();
                    if (!AllowedExt.Contains(ext))
                    {
                        notifyService.Error($"Invalid Document image type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file type.");
                        await LoadModel(model);
                        return View(model);
                    }

                    if (!AllowedMime.Contains(model.Document.ContentType))
                    {
                        notifyService.Error($"Invalid Document Image content type");
                        ModelState.AddModelError(nameof(model.Document), "Invalid Document Image  content type.");
                        await LoadModel(model);
                        return View(model);
                    }

                    if (!ImageSignatureValidator.HasValidSignature(model.Document))
                    {
                        notifyService.Error($"Invalid or corrupted Document Image ");
                        ModelState.AddModelError(nameof(model.Document), "Invalid file content.");
                        await LoadModel(model);
                        return View(model);
                    }
                }
                if (vendorId != model.VendorId || model.SelectedCountryId < 1 || model.SelectedStateId < 1 || model.SelectedDistrictId < 1 || model.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Edit), "Vendors", new { id = vendorId });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var edited = await agencyService.EditAgency(model, currentUserEmail, portal_base_url);

                if (edited)
                {
                    notifyService.Custom($"Agency <b>{model.Email}</b> edited successfully.", 3, "orange", "fas fa-building");
                    return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
            }
            notifyService.Error("OOPS !!!..Error editing Agency. Try again.");
            return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
        }

        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                if (id < 1 || _context.Vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var agencySubStatuses = new[]{
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR };

                var hasClaims = _context.Investigations.Any(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == id);
                var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Manager Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("AvailableVendors", "Vendors", "Available Agencies") { Parent = agencysPage, };
                var editPage = new MvcBreadcrumbNode("Delete", "Vendors", $"Delete Agency") { Parent = agencyPage };
                ViewData["BreadcrumbNode"] = editPage;
                vendor.HasClaims = hasClaims;
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long VendorId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var vendor = await _context.Vendor.FindAsync(VendorId);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Agency Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorUser = await _context.VendorApplicationUser.Where(v => v.VendorId == VendorId).ToListAsync();
                foreach (var user in vendorUser)
                {
                    user.Updated = DateTime.Now;
                    user.UpdatedBy = currentUserEmail;
                    user.Deleted = true;
                    _context.VendorApplicationUser.Update(user);
                }
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = currentUserEmail;
                vendor.Deleted = true;
                _context.Vendor.Update(vendor);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Agency <b>{vendor.Email}</b> deleted successfully.", 3, "red", "fas fa-building");
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                return RedirectToAction(nameof(AvailableVendors), "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}