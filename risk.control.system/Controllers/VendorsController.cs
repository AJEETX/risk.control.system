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
        private readonly ApplicationDbContext _context;
        private readonly ITinyUrlService urlService;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly INotyfService notifyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ISmsService smsService;
        private readonly IInvestigationService service;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IFeatureManager featureManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<VendorsController> logger;
        private string portal_base_url = string.Empty;

        public VendorsController(
            ApplicationDbContext context,
            ITinyUrlService urlService,
            UserManager<VendorApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            INotyfService notifyService,
            ICustomApiCLient customApiCLient,
            ISmsService SmsService,
            IInvestigationService service,
            IFeatureManager featureManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<VendorsController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
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
            this.webHostEnvironment = webHostEnvironment;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        // GET: Vendors
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
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Not Agency Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
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
                notifyService.Custom($"Agency(s) de-panelled.", 3, "orange", "far fa-thumbs-down");
                return RedirectToAction("EmpanelledVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
            if (vendors is null || vendors.Count == 0)
            {
                notifyService.Error("No agency selected !!!");
                return RedirectToAction(nameof(AvailableVendors), "Company");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendors2Empanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                company.EmpanelledVendors.AddRange(vendors2Empanel.ToList());

                company.Updated = DateTime.Now;
                company.UpdatedBy = currentUserEmail;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                notifyService.Custom($"Agency(s) empanelled.", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction("AvailableVendors", "Vendors");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        public JsonResult PostRating(int rating, long mid)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var existingRating = _context.Ratings.FirstOrDefault(r => r.VendorId == mid && r.UserEmail == currentUserEmail);
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

        public JsonResult PostDetailRating(int rating, long vendorId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Json(new { success = false, message = "You must be logged in to rate." });
            }

            var existingRating = _context.Ratings.FirstOrDefault(r => r.VendorId == vendorId && r.UserEmail == currentUserEmail);

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

        // GET: Vendors/Details/5
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

                var vendorUserCount = await _context.VendorApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted && c.Role == AppRoles.AGENT);

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
        public IActionResult CreateUser(long id)
        {
            if (id <= 0)
            {
                notifyService.Custom($"OOPs !!!..Error creating user.", 3, "red", "fa fa-user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var allRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>()?.ToList();
            AgencyRole? role = null;
            string? adminEmail = null;
            var vendor = _context.Vendor.Include(v => v.Country).FirstOrDefault(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentVendorUserCount = _context.VendorApplicationUser.Count(v => v.VendorId == id);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                adminEmail = "admin";
                role = AgencyRole.AGENCY_ADMIN;
                status = true;
                allRoles = allRoles.Where(r => r == AgencyRole.AGENCY_ADMIN).ToList();
            }
            else
            {
                allRoles = allRoles.Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            }
            var model = new VendorApplicationUser { Email = adminEmail, Active = status, Country = vendor.Country, CountryId = vendor.CountryId, Vendor = vendor, AgencyRole = allRoles, UserRole = role };

            var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("CreateUser", "Vendors", $"Add User") { Parent = usersPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        // POST: VendorApplicationUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser user, string emailSuffix, string createdBy = "")
        {
            if (user is null || string.IsNullOrEmpty(emailSuffix) || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Vendors", new { userid = user.Id });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (user.ProfileImage != null && user.ProfileImage.Length > 0)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                    user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    user.ProfilePictureUrl = "/agency/" + newFileName;

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();
                    user.ProfilePictureExtension = fileExtension;
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                //DEMO
                user.Password = Applicationsettings.Password;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.PhoneNumber = user.PhoneNumber.TrimStart('0');
                user.PinCodeId = user.SelectedPincodeId;
                user.DistrictId = user.SelectedDistrictId;
                user.StateId = user.SelectedStateId;
                user.CountryId = user.SelectedCountryId;

                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.Updated = DateTime.Now;
                user.UpdatedBy = currentUserEmail;
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                if (user.Role == AppRoles.AGENT)
                {
                    var pincode = _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(c => c.PinCodeId == user.PinCodeId);
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
                    var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + portal_base_url);
                            notifyService.Custom($"User created.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {

                        await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + portal_base_url);

                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);

                        if (onboardAgent)
                        {
                            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

                            string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                            var message = $"Dear {user.FirstName}\n";
                            message += $"Click on link below to install the mobile app\n\n";
                            message += $"{tinyUrl}\n\n";
                            message += $"Thanks\n\n";
                            message += $"{portal_base_url}";

                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, message, true);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "Agency user created. \nEmail : " + user.Email + "\n" + portal_base_url);
                        }
                        notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(Users), "Vendors", new { id = user.VendorId });
                }

                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
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
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser applicationUser, string editby)
        {
            if (applicationUser is null || applicationUser.SelectedCountryId < 1 || applicationUser.SelectedStateId < 1 || applicationUser.SelectedDistrictId < 1 || applicationUser.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(EditUser), "Vendors", new { userid = id });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(id) || applicationUser is null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(applicationUser.ProfileImage.FileName));
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
                    applicationUser.ProfilePictureExtension = fileExtension;
                }
                user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
                user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.ProfilePictureExtension = applicationUser?.ProfilePictureExtension ?? user.ProfilePictureExtension;
                user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                user.PhoneNumber = user.PhoneNumber.TrimStart('0');
                user.FirstName = applicationUser?.FirstName;
                user.LastName = applicationUser?.LastName;
                if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                {
                    user.Password = applicationUser.Password;
                }
                user.Addressline = applicationUser.Addressline;
                user.Active = applicationUser.Active;

                user.PinCodeId = applicationUser.SelectedPincodeId;
                user.DistrictId = applicationUser.SelectedDistrictId;
                user.StateId = applicationUser.SelectedStateId;
                user.CountryId = applicationUser.SelectedCountryId;

                user.IsUpdated = true;
                user.Updated = DateTime.Now;
                user.Comments = applicationUser.Comments;
                user.PhoneNumber = applicationUser.PhoneNumber;
                user.UpdatedBy = currentUserEmail;
                user.SecurityStamp = DateTime.Now.ToString();
                user.UserRole = applicationUser.UserRole;
                user.IsVendorAdmin = user.UserRole == null;
                user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());

                if (user.Role == AppRoles.AGENT)
                {
                    var pincode = _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(c => c.PinCodeId == user.PinCodeId);
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
                    var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
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
                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);
                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            if (onboardAgent)
                            {
                                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);
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
                                notifyService.Custom($"User edited.", 3, "orange", "fas fa-user-check");
                            }
                        }
                    }

                    if (editby == "company")
                    {
                        return RedirectToAction(nameof(Users), "Vendors", new { id = applicationUser.VendorId });
                    }
                    else if (editby == "empanelled")
                    {
                        return RedirectToAction(nameof(CompanyController.AgencyUsers), "Company", new { id = applicationUser.VendorId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(AgencyController.Users), "Agency");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
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
                notifyService.Custom($"User {model.Email} deleted", 3, "red", "fas fa-user-minus");
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

        // GET: VendorApplicationUsers/Delete/5
        [Breadcrumb(" Roles", FromAction = "EditUser")]
        public async Task<IActionResult> UserRoles(string userId)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var userRoles = new List<VendorUserRoleViewModel>();
            //ViewBag.userId = userId;
            VendorApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                notifyService.Error("User Not Found!");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            //ViewBag.UserName = user.UserName;
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

            result = await userManager.AddToRolesAsync(user, model.VendorUserRoleViewModel.
                Where(x => x.Selected).Select(y => y.RoleName));
            var onboardAgent = newRoles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId) && user.Active;
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);
            string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

            var message = $"Dear {user.FirstName}\n";
            message += $"Please click on the link below to install the mobile\n";
            message += $"{tinyUrl}\n";
            message += $"{portal_base_url}";
            if (onboardAgent)
            {
                notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
            }
            else
            {
                notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            }
            return RedirectToAction("Users", "Vendors", new { id = model.VendorId });
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

        // GET: Vendors/Create
        [Breadcrumb(" Add Agency")]
        public IActionResult Create()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
            var vendor = new Vendor { CountryId = companyUser.ClientCompany.CountryId, Country = companyUser.ClientCompany.Country, SelectedCountryId = companyUser.ClientCompany.CountryId.Value };
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor, string domainAddress, string mailAddress)
        {
            if (vendor is null || string.IsNullOrWhiteSpace(domainAddress) || string.IsNullOrWhiteSpace(mailAddress) || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Create), "Vendors");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

                vendor.Email = mailAddress.ToLower() + domainData.GetEnumDisplayName();

                IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                if (vendorDocument is not null)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(vendorDocument.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);
                    vendorDocument.CopyTo(new FileStream(upload, FileMode.Create));
                    vendor.DocumentUrl = "/agency/" + newFileName;

                    using var dataStream = new MemoryStream();
                    vendorDocument.CopyTo(dataStream);
                    vendor.DocumentImage = dataStream.ToArray();
                    vendor.DocumentImageExtension = fileExtension;
                }
                vendor.Status = VendorStatus.ACTIVE;
                vendor.AgreementDate = DateTime.Now;
                vendor.ActivatedDate = DateTime.Now;
                vendor.DomainName = domainData;
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = currentUserEmail;
                vendor.CreatedUser = currentUserEmail;
                vendor.PhoneNumber = vendor.PhoneNumber.TrimStart('0');
                vendor.PinCodeId = vendor.SelectedPincodeId;
                vendor.DistrictId = vendor.SelectedDistrictId;
                vendor.StateId = vendor.SelectedStateId;
                vendor.CountryId = vendor.SelectedCountryId;

                var pinCode = _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.PinCodeId == vendor.SelectedPincodeId);

                var companyAddress = vendor.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
                var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
                var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                vendor.AddressLatitude = companyCoordinates.Latitude;
                vendor.AddressLongitude = companyCoordinates.Longitude;
                vendor.AddressMapLocation = url;
                _context.Add(vendor);

                var managerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.MANAGER.ToString()));
                var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);

                var notification = new StatusNotification
                {
                    Role = managerRole,
                    Company = companyUser.ClientCompany,
                    Symbol = "far fa-hand-point-right i-orangered",
                    Message = $"Agency {vendor.Email} created",
                    Status = "Empanel",
                    NotifierUserEmail = currentUserEmail
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    await smsService.DoSendSmsAsync(pinCode.Country.Code, pinCode.Country.ISDCode + vendor.PhoneNumber, "Agency created. \nDomain : " + vendor.Email + "\n" + portal_base_url);
                }

                notifyService.Custom($"Agency created successfully.", 3, "green", "fas fa-building");
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
        public async Task<IActionResult> Edit(long vendorId, Vendor vendor)
        {
            if (vendor is null || vendorId != vendor.VendorId || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Edit), "Vendors", new { id = vendorId });
            }

            try
            {
                IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                if (vendorDocument is not null)
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(vendorDocument.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);

                    using var dataStream = new MemoryStream();
                    vendorDocument.CopyTo(dataStream);
                    vendor.DocumentImage = dataStream.ToArray();
                    vendorDocument.CopyTo(new FileStream(upload, FileMode.Create));
                    vendor.DocumentUrl = "/agency/" + newFileName;
                    vendor.DocumentImageExtension = fileExtension;
                }
                else
                {
                    var existingVendor = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorId);
                    if (existingVendor.DocumentImage != null || existingVendor.DocumentUrl != null)
                    {
                        vendor.DocumentImage = existingVendor.DocumentImage;
                        vendor.DocumentUrl = existingVendor.DocumentUrl;
                    }
                }
                vendor.IsUpdated = true;
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;
                vendor.PhoneNumber = vendor.PhoneNumber.TrimStart('0');

                vendor.PinCodeId = vendor.SelectedPincodeId;
                vendor.DistrictId = vendor.SelectedDistrictId;
                vendor.StateId = vendor.SelectedStateId;
                vendor.CountryId = vendor.SelectedCountryId;
                var pinCode = _context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.PinCodeId == vendor.SelectedPincodeId);

                var companyAddress = vendor.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
                var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
                var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                vendor.AddressLatitude = companyCoordinates.Latitude;
                vendor.AddressLongitude = companyCoordinates.Longitude;
                vendor.AddressMapLocation = url;

                _context.Vendor.Update(vendor);

                await smsService.DoSendSmsAsync(pinCode.Country.Code, pinCode.Country.ISDCode + vendor.PhoneNumber, "Agency edited. \nDomain : " + vendor.Email + "\n" + portal_base_url);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Agency edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
        }

        // GET: Vendors/Delete/5
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

        // POST: Vendors/Delete/5
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
                notifyService.Custom($"Agency {vendor.Email} deleted successfully.", 3, "red", "fas fa-building");
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                if (superAdminUser.IsSuperAdmin)
                {
                    return RedirectToAction(nameof(Agencies), "Vendors");
                }
                else
                {
                    return RedirectToAction(nameof(CompanyController.AvailableVendors), "Company");
                }
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