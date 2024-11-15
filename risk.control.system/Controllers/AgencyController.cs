using System.Net;

using Amazon.Rekognition.Model;

using AspNetCoreHero.ToastNotification.Abstractions;

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

using SixLabors.ImageSharp.ColorSpaces;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    public class AgencyController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IDashboardService dashboardService;
        private readonly ISmsService smsService;
        private readonly IToastNotification toastNotification;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgencyController(ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IDashboardService dashboardService,
            ISmsService SmsService,
            IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.dashboardService = dashboardService;
            smsService = SmsService;
            this.toastNotification = toastNotification;
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
                    notifyService.Error("User Not Found!!!..Contact Admin");
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
                    notifyService.Error("User Not found!!!..Contact Admin");
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

                var country = _context.Country.OrderBy(c => c.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendor.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendor.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendor.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", vendor.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", vendor.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendor.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", vendor.PinCodeId);

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
            try
            {
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
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (vendorUser == null)
                {
                    notifyService.Error("User Not found !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();


                if (vendorDocument is not null)
                {
                    string newFileName = vendor.Email + Guid.NewGuid().ToString();
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
                }
                else
                {
                    var existingVendor = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                    if (existingVendor.DocumentImage != null || existingVendor.DocumentUrl != null)
                    {
                        vendor.DocumentImage = existingVendor.DocumentImage;
                        vendor.DocumentUrl = existingVendor.DocumentUrl;
                    }
                }
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Vendor.Update(vendor);
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(vendor.PhoneNumber, "Agency account created. Domain : " + vendor.Email);

                notifyService.Custom($"Agency {vendor.Email} edited successfully.", 3, "green", "fas fa-building");
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
                var model = new VendorApplicationUser { Vendor = vendor };
                ViewData["CountryId"] = new SelectList(_context.Country.OrderBy(c => c.Name), "CountryId", "Name");
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
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.Role = user.Role != null ? user.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                IdentityResult result = await userManager.CreateAsync(user, user.Password);
                if (result.Succeeded)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                    roleResult = await userManager.AddToRolesAsync(user, new List<string> { user.UserRole.ToString() });
                    
                    if (!user.Active)
                    {
                        var createdUser = await userManager.FindByEmailAsync(user.Email);
                        var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            notifyService.Custom($"User {user.Email} created and locked.", 3, "orange", "fas fa-user-lock");
                            await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user created and locked. Email : " + user.Email);
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
                                await smsService.DoSendSmsAsync(user.PhoneNumber, message);
                                notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                            }
                            else
                            {
                                await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
                                notifyService.Custom($"User {user.Email} edited.", 3, "green", "fas fa-user-check");
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
                GetCountryStateEdit(user);
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

                var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(userId);
                if (vendorApplicationUser == null)
                {
                    notifyService.Error("User not found!!!..Contact Admin");
                    return RedirectToAction(nameof(AgencyController.User), "Agency");
                }
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

                ViewBag.Show = vendorApplicationUser.Email == vendorUser.Email ? false : true;

                vendorApplicationUser.Vendor = vendor;

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendorApplicationUser.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendorApplicationUser.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendorApplicationUser.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", vendorApplicationUser.CountryId);
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
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser applicationUser)
        {
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
                    user.Addressline = applicationUser.Addressline;
                    user.Active = applicationUser.Active;
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
                    user.UserRole = applicationUser.UserRole;
                    user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                    user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;

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
                                await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user edited and locked. Email : " + user.Email);
                                notifyService.Custom($"User {user.Email} edited and locked.", 3, "orange", "fas fa-user-lock");
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
                                    await smsService.DoSendSmsAsync(user.PhoneNumber, message);
                                    notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                                }
                                else
                                {
                                    await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
                                    notifyService.Custom($"User {user.Email} edited.", 3, "green", "fas fa-user-check");
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

        [Breadcrumb("Edit Role", FromAction = "Users")]
        public async Task<IActionResult> UserRoles(string userId)
        {
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
                var userEmail = HttpContext.User?.Identity?.Name;
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);

                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
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
            try
            {
                if(vendorInvestigationServiceType is null)
                {
                    notifyService.Custom($"Error to create service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User?.Identity?.Name;
                if (userEmail is null)
                {
                    notifyService.Custom($"Error to create service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                if (vendorUser == null)
                {
                    notifyService.Custom($"Error to create service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);
                if (vendor == null)
                {
                    notifyService.Custom($"Error to create service.", 3, "red", "fas fa-truck");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (vendorInvestigationServiceType is not null)
                {
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
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                    vendorInvestigationServiceType.Created = DateTime.Now;
                    vendorInvestigationServiceType.VendorId = vendor.VendorId;
                    _context.Add(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service created successfully.", 3, "green", "fas fa-truck");

                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
                ViewData["CountryId"] = new SelectList(_context.Country.OrderBy(c => c.Name), "CountryId", "Name", vendorInvestigationServiceType.CountryId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", vendorInvestigationServiceType.DistrictId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
                notifyService.Error("OOPS !!!..Contact Admin");
                return View(vendorInvestigationServiceType);
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
                var userEmail = HttpContext.User?.Identity?.Name;
                if (userEmail is null)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
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

                var country = _context.Country.OrderBy(o => o.Name);
                var states = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendorInvestigationServiceType.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendorInvestigationServiceType.StateId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", vendorInvestigationServiceType.CountryId);
                ViewData["StateId"] = new SelectList(states, "StateId", "Name", vendorInvestigationServiceType.StateId);
                ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendorInvestigationServiceType.DistrictId);

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
            try
            {
                if (vendorInvestigationServiceTypeId != vendorInvestigationServiceType.VendorInvestigationServiceTypeId)
                {
                    notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
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
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Update(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service updated successfully.", 3, "orange", "fas fa-truck");
                    return RedirectToAction(nameof(AgencyController.Service), "Agency");
                }
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", vendorInvestigationServiceType.DistrictId);
                ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
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
                    notifyService.Custom($"Service deleted successfully.", 3, "blue", "fas fa-truck");
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
            return View();
        }

        private void GetCountryStateEdit(VendorApplicationUser? user)
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", user?.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", user?.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State.Where(s => s.CountryId == user.CountryId), "StateId", "Name", user?.StateId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode.Where(s => s.StateId == user.StateId), "PinCodeId", "Name", user?.PinCodeId);
        }
    }
}