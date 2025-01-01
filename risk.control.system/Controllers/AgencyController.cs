using System.Net;

using AspNetCoreHero.ToastNotification.Abstractions;

using Google.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using Org.BouncyCastle.Utilities.Net;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Company;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SixLabors.ImageSharp.ColorSpaces;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME}")]
    public class AgencyController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IDashboardService dashboardService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ISmsService smsService;
        private readonly IToastNotification toastNotification;
        private readonly IAgencyService agencyService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgencyController(ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IDashboardService dashboardService,
            ICustomApiCLient customApiCLient,
            ISmsService SmsService,
            IToastNotification toastNotification,
            IAgencyService agencyService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.dashboardService = dashboardService;
            this.customApiCLient = customApiCLient;
            smsService = SmsService;
            this.toastNotification = toastNotification;
            this.agencyService = agencyService;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
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
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

                var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.PincodeServices)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.LineOfBusiness)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!...Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        // GET: Vendors/Edit/5
        [Breadcrumb("Edit Agency", FromAction = "Profile")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = await _context.Vendor.FindAsync(vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"Agency Not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: Vendors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor vendor)
        {
            if(vendor is null || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
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
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("User Not found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();

                var edited = await agencyService.EditAgency(vendor, vendorDocument, currentUserEmail);
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("User Not found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Add User")]
        public IActionResult CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("User Not found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }
                var roles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>().Where(role => role != AgencyRole.AGENCY_ADMIN)?.ToList();

                var model = new VendorApplicationUser { Vendor = vendor, AgencyRole = roles };
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser user, string emailSuffix, string vendorId, string txn = "agency")
        {
            if(user is null || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Agency");
            }

            try
            {
                if(user == null || string.IsNullOrWhiteSpace(emailSuffix) || string.IsNullOrWhiteSpace(vendorId))
                {
                    notifyService.Custom($"Error to create user.", 3, "red", "fas fa-user-plus");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                if (user.ProfileImage != null && user.ProfileImage.Length > 0 && !string .IsNullOrWhiteSpace(Path.GetFileName(user.ProfileImage.FileName)))
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
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                //DEMO
                user.Password = Applicationsettings.Password;

                user.PinCodeId = user.SelectedPincodeId;
                user.DistrictId = user.SelectedDistrictId;
                user.StateId = user.SelectedStateId;
                user.CountryId = user.SelectedCountryId;

                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.Role = user.Role != null ? user.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                if(user.Role == AppRoles.AGENT)
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

                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            notifyService.Custom($"User {user.Email} created.", 3, "green", "fas fa-user-lock");
                            await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user created. Email : " + user.Email);
                            if(txn =="agency")
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
                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);
                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            

                            if (onboardAgent)
                            {
                                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

                                System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + vendor.MobileAppUrl);
                                System.Net.WebClient client = new System.Net.WebClient();
                                string tinyUrl = client.DownloadString(address);

                                var message = $"Dear {user.FirstName}";
                                message += "                                                                                ";
                                message += $"Click on link below to install the mobile app";
                                message += "                                                                                ";
                                message += $"{tinyUrl}";
                                message += "                                                                                ";
                                message += $"Thanks";
                                message += "                                                                                ";
                                message += $"https://icheckify.co.in";
                                await smsService.DoSendSmsAsync(user.PhoneNumber, message, true);
                                notifyService.Custom($"Agent {user.Email} onboarding initiated.", 3, "green", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(user.PhoneNumber, "User created. Email : " + user.Email);
                                notifyService.Custom($"User {user.Email} created.", 3, "green", "fas fa-user-check");
                            }
                        }

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
                    foreach (IdentityError error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
                notifyService.Custom($"Error to create user.", 3, "red", "fas fa-user-plus");
                return View(user);
            }
            catch (Exception ex)
            {
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
                if (userId == null || userId < 1)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }

                var user = await _context.VendorApplicationUser.FindAsync(userId);
                if (user == null)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"No agency not found.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(AgencyController.Profile), "Agency");
                }

                user.Vendor = vendor;
                return View(user);
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
                    notifyService.Error("Err !!! bad Request");
                    return RedirectToAction(nameof(AgencyController.Users), "Agency");
                }
                var user = await userManager.FindByIdAsync(id);
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
                    using var dataStream = new MemoryStream();
                    applicationUser.ProfileImage.CopyTo(dataStream);
                    applicationUser.ProfilePicture = dataStream.ToArray();
                    applicationUser.ProfilePictureUrl = "/agency/" + newFileName;
                }

                if (user != null)
                {
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
                    user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
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
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
                    user.UserRole = applicationUser.UserRole;
                    user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
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
                                await smsService.DoSendSmsAsync(user.PhoneNumber, "User edited. Email : " + user.Email);
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

                                    System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + vendor.MobileAppUrl);
                                    System.Net.WebClient client = new System.Net.WebClient();
                                    string tinyUrl = client.DownloadString(address);

                                    var message = $"Dear {user.FirstName}";
                                    message += "                                                                                ";
                                    message += $"Click on link below to install the mobile app";
                                    message += "                                                                                ";
                                    message += $"{tinyUrl}";
                                    message += "                                                                                ";
                                    message += $"Thanks";
                                    message += "                                                                                ";
                                    message += $"https://icheckify.co.in";
                                    await smsService.DoSendSmsAsync(user.PhoneNumber, message, true);
                                    notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                                }
                                else
                                {
                                    await smsService.DoSendSmsAsync(user.PhoneNumber, "User edited and unlocked. Email : " + user.Email);
                                    notifyService.Custom($"User {user.Email} edited.", 3, "orange", "fas fa-user-check");
                                }
                            }
                        }
                        return RedirectToAction(nameof(AgencyController.Users), "Agency");
                    }
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
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await _context.VendorApplicationUser.Include(v=>v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencySubStatuses = _context.InvestigationCaseSubStatus.Where(i =>
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR) ||
                    i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
                    ).Select(s => s.InvestigationCaseSubStatusId).ToList();

                var hasClaims = _context.ClaimsInvestigation.Any(c => agencySubStatuses.Contains(c.InvestigationCaseSubStatus.InvestigationCaseSubStatusId) && c.VendorId == model.VendorId);
                model.HasClaims = hasClaims;
                return View(model);
            }
            catch (Exception ex)
            {
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
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (string.IsNullOrWhiteSpace(email))
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = _context.VendorApplicationUser.Include(v=>v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefault(c => c.Email == email);
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
                toastNotification.AddErrorToastMessage("user not found!");
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
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
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
                return NotFound();
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

            System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + vendor.MobileAppUrl);
            System.Net.WebClient client = new System.Net.WebClient();
            string tinyUrl = client.DownloadString(address);

            var message = $"Dear {user.FirstName}";
            message += "                                                                                ";
            message += $"Click on link below to install the mobile app";
            message += "                                                                                ";
            message += $"{tinyUrl}";
            message += "                                                                                ";
            message += $"Thanks";
            message += "                                                                                ";
            message += $"https://icheckify.co.in";
            if (onboardAgent)
            {
                await smsService.DoSendSmsAsync(user.PhoneNumber, message);
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
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);

                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                var model = new VendorInvestigationServiceType { SelectedMultiPincodeId = new List<long>(), Vendor = vendor, PincodeServices = new List<ServicedPinCode>() };
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType vendorInvestigationServiceType)
        {
            if (vendorInvestigationServiceType is null || vendorInvestigationServiceType.SelectedCountryId < 1 || vendorInvestigationServiceType.SelectedStateId < 1 || vendorInvestigationServiceType.SelectedDistrictId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateService), "Agency");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                var pincodesServiced = await _context.PinCode.Where(p => vendorInvestigationServiceType.SelectedMultiPincodeId.Contains(p.PinCodeId)).ToListAsync();
                var servicePinCodes = pincodesServiced.Select(p =>
                new ServicedPinCode
                {
                    Name = p.Name,
                    Pincode = p.Code,
                    VendorInvestigationServiceTypeId = vendorInvestigationServiceType.VendorInvestigationServiceTypeId,
                    VendorInvestigationServiceType = vendorInvestigationServiceType,
                }).ToList();
                vendorInvestigationServiceType.PincodeServices = servicePinCodes;
                
                vendorInvestigationServiceType.VendorId = vendorUser.VendorId.GetValueOrDefault();
                vendorInvestigationServiceType.CountryId = vendorInvestigationServiceType.SelectedCountryId;
                vendorInvestigationServiceType.StateId = vendorInvestigationServiceType.SelectedStateId;
                vendorInvestigationServiceType.DistrictId = vendorInvestigationServiceType.SelectedDistrictId;

                vendorInvestigationServiceType.Updated = DateTime.Now;
                vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                vendorInvestigationServiceType.Created = DateTime.Now;
                _context.Add(vendorInvestigationServiceType);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Service created successfully.", 3, "green", "fas fa-truck");

                return RedirectToAction(nameof(AgencyController.Service), "Agency");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Service", FromAction = "Service")]
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);

                if (vendorInvestigationServiceType is null)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var services = _context.VendorInvestigationServiceType
                    .Include(v => v.Vendor)
                    .Include(v => v.PincodeServices)
                    .First(v => v.VendorInvestigationServiceTypeId == id);

                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType
                    .Include(i => i.LineOfBusiness)
                    .Where(i => i.LineOfBusiness.LineOfBusinessId == vendorInvestigationServiceType.LineOfBusinessId),
                    "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);

                ViewBag.PinCodeId = _context.PinCode.Where(p => p.District.DistrictId == vendorInvestigationServiceType.DistrictId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name + " - " + x.Code,
                        Value = x.PinCodeId.ToString()
                    }).ToList();

                var selected = services.PincodeServices.Select(s => s.Pincode).ToList();
                services.SelectedMultiPincodeId = _context.PinCode.Where(p => selected.Contains(p.Code)).Select(p => p.PinCodeId).ToList();

                return View(services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        // POST: VendorService/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long vendorInvestigationServiceTypeId, VendorInvestigationServiceType vendorInvestigationServiceType)
        {
            if (vendorInvestigationServiceType is null || vendorInvestigationServiceType.SelectedCountryId < 1 || vendorInvestigationServiceType.SelectedStateId < 1 || vendorInvestigationServiceType.SelectedDistrictId < 1)
            {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(EditService), "Agency",new {id = vendorInvestigationServiceTypeId });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (vendorInvestigationServiceTypeId != vendorInvestigationServiceType.VendorInvestigationServiceTypeId)
                {
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                if (vendorInvestigationServiceType.SelectedMultiPincodeId.Count > 0)
                {
                    var existingServicedPincodes = _context.ServicedPinCode.Where(s => s.VendorInvestigationServiceTypeId == vendorInvestigationServiceType.VendorInvestigationServiceTypeId);
                    _context.ServicedPinCode.RemoveRange(existingServicedPincodes);

                    var pinCodeDetails = _context.PinCode.Where(p => vendorInvestigationServiceType.SelectedMultiPincodeId.Contains(p.PinCodeId));

                    var pinCodesWithId = pinCodeDetails.Select(p => new ServicedPinCode
                    {
                        Pincode = p.Code,
                        Name = p.Name,
                        VendorInvestigationServiceTypeId = vendorInvestigationServiceType.VendorInvestigationServiceTypeId
                    }).ToList();
                    _context.ServicedPinCode.AddRange(pinCodesWithId);

                    vendorInvestigationServiceType.PincodeServices = pinCodesWithId;
                    
                    vendorInvestigationServiceType.CountryId = vendorInvestigationServiceType.SelectedCountryId;
                    vendorInvestigationServiceType.StateId = vendorInvestigationServiceType.SelectedStateId;
                    vendorInvestigationServiceType.DistrictId = vendorInvestigationServiceType.SelectedDistrictId;


                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = currentUserEmail;
                    vendorInvestigationServiceType.IsUpdated = true; 
                    _context.Update(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service updated successfully.", 3, "orange", "fas fa-truck");
                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        // GET: VendorService/Delete/5
        [Breadcrumb("Delete Service", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Custom($"NOT FOUND.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.LineOfBusiness)
                    .Include(v => v.PincodeServices)
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: VendorService/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (_context.VendorInvestigationServiceType == null || id == 0)
                {
                    return Problem("Entity set 'ApplicationDbContext.VendorInvestigationServiceType'  is null.");
                }
                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType != null)
                {
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
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
                Console.WriteLine(ex.ToString());
                notifyService.Custom($"Error to delete service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Services", FromAction = "Service")]
        public async Task<IActionResult> ServiceDetail(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Error("NOT FOUND !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                    .Include(v => v.InvestigationServiceType)
                    .Include(v => v.LineOfBusiness)
                    .Include(v => v.PincodeServices)
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