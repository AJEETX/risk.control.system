using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers
{
    public class VendorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private readonly IWebHostEnvironment webHostEnvironment;

        public VendorsController(
            ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            INotyfService notifyService,
            IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
            this.webHostEnvironment = webHostEnvironment;
        }

        // GET: Vendors
        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Index()
        {
            return RedirectToAction("Agencies");
        }

        [Breadcrumb("All Agencies", FromAction = "Index")]
        public IActionResult Agencies()
        {
            return View();
        }

        public JsonResult PostRating(int rating, long mid)
        {
            //save data into the database

            var rt = new AgencyRating();
            string ip = "123";
            rt.Rate = rating;
            rt.IpAddress = ip;
            rt.VendorId = mid;

            //save into the database
            _context.Ratings.Add(rt);
            _context.SaveChanges();
            return Json("You rated this " + rating.ToString() + " star(s)");
        }

        // GET: Vendors/Details/5
        [Breadcrumb(" Manage Agency", FromAction = "Agencies")]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                if (id < 1 || _context.Vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }


                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb(" Manage Users", FromAction = "Details")]
        public IActionResult Users(string id)
        {
            ViewData["vendorId"] = id;

            var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("Agencies", "Vendors", "All Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View();
        }

        [Breadcrumb(" Add User", FromAction = "Users")]
        public IActionResult CreateUser(long id)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == id);
            var model = new VendorApplicationUser { Vendor = vendor };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("Agencies", "Vendors", "All Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
            var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("CreateUser", "Vendors", $"Add User") { Parent = usersPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        // POST: VendorApplicationUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser user, string emailSuffix)
        {
            try
            {
                if (string.IsNullOrEmpty(emailSuffix) || user is null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                //DEMO
                user.Password = Applicationsettings.Password;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };
                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
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
                            var response = SmsService.SendSingleMessage(user.PhoneNumber, "Agency user created and locked. Email : " + user.Email);
                            notifyService.Custom($"User edited and locked.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {

                        var response = SmsService.SendSingleMessage(user.PhoneNumber, "Agency user created. Email : " + user.Email);

                        var onboardAgent = roles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);
                        
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

                            var onboard = SmsService.SendSingleMessage(user.PhoneNumber, message, onboardAgent);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            SmsService.SendSingleMessage(user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
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
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Edit User", FromAction = "Users")]
        public IActionResult EditUser(long? userId)
        {
            try
            {
                if (userId == null || _context.VendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorApplicationUser = _context.VendorApplicationUser
                    .Include(v => v.Mailbox).Where(v => v.Id == userId)
                    ?.FirstOrDefault();
                if (vendorApplicationUser == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorApplicationUser.VendorId);

                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
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



                var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("Agencies", "Vendors", "All Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = vendor.VendorId } };
                var usersPage = new MvcBreadcrumbNode("Users", "Vendors", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = vendor.VendorId } };
                var editPage = new MvcBreadcrumbNode("EditUser", "Vendors", $"Edit User") { Parent = usersPage, RouteValues = new { id = userId } };
                ViewData["BreadcrumbNode"] = editPage;


                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser applicationUser)
        {
            try
            {
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
                    string newFileName = user.Email + Guid.NewGuid().ToString();
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
                }
                user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
                user.ProfileImage = applicationUser?.ProfileImage ?? user.ProfileImage;
                user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
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
                user.IsVendorAdmin = user.UserRole == AgencyRole.AGENCY_ADMIN;
                user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());

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
                            var response = SmsService.SendSingleMessage(user.PhoneNumber, "Agency user edited and locked. Email : " + user.Email);
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
                                var onboard = SmsService.SendSingleMessage(user.PhoneNumber, message, onboardAgent);
                                notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                            }
                            else
                            {
                                var response = SmsService.SendSingleMessage(user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
                                notifyService.Custom($"User edited.", 3, "green", "fas fa-user-check");
                            }
                        }
                    }
                    return RedirectToAction(nameof(Users), "Vendors", new { id = applicationUser.VendorId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            notifyService.Error("OOPS !!!..Contact Admin");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        // GET: VendorApplicationUsers/Delete/5
        [Breadcrumb(" Roles", FromAction = "EditUser")]
        public async Task<IActionResult> UserRoles(string userId)
        {
            var userRoles = new List<VendorUserRoleViewModel>();
            //ViewBag.userId = userId;
            VendorApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
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
            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "All Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = user.VendorId } };
            //var usersPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = user.VendorId } };
            //var editPage = new MvcBreadcrumbNode("UserRoles", "VendorApplicationUsers", $"Edit Role") { Parent = usersPage, RouteValues = new { id = user.Id } };
            //ViewData["BreadcrumbNode"] = editPage;
            return View(model);
        }

        [HttpPost]
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

            result = await userManager.AddToRolesAsync(user, model.VendorUserRoleViewModel.
                Where(x => x.Selected).Select(y => y.RoleName));
            var onboardAgent = newRoles.Any(r => AppConstant.AppRoles.AGENT.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId) && user.Active;
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == user.VendorId);

            System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + vendor.MobileAppUrl);
            System.Net.WebClient client = new System.Net.WebClient();
            string tinyUrl = client.DownloadString(address);
            var message = $"Dear {user.FirstName}";
            message += "                                                                                ";
            message += $"Please click on the link below to install the mobile";
            message += "                                                                                ";
            message += $"{tinyUrl}";
            message += "                                                                                ";
            message += $"Thanks";
            message += "                                                                                ";
            message += $"https://icheckify.co.in";
            if (onboardAgent)
            {
                var onboard = SmsService.SendSingleMessage(user.PhoneNumber, message, onboardAgent);
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
            if (id == null || _context.Vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewData["vendorId"] = id;

            var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manager Agency(s)");
            var agency2Page = new MvcBreadcrumbNode("Agencies", "Vendors", "All Agencies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
            var servicesPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manager Service") { Parent = agencyPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = servicesPage;

            return View();
        }

        // GET: Vendors/Create
        [Breadcrumb(" Add Agency")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor, string domainAddress, string mailAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(domainAddress) || string.IsNullOrWhiteSpace(mailAddress) || vendor is null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

                vendor.Email = mailAddress.ToLower() + domainData.GetEnumDisplayName();

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
                    vendorDocument.CopyTo(new FileStream(upload, FileMode.Create));
                    vendor.DocumentUrl = "/agency/" + newFileName;

                    using var dataStream = new MemoryStream();
                    vendorDocument.CopyTo(dataStream);
                    vendor.DocumentImage = dataStream.ToArray();
                }
                vendor.Status = VendorStatus.ACTIVE;
                vendor.ActivatedDate = DateTime.Now;
                vendor.DomainName = domainData;
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

                _context.Add(vendor);
                await _context.SaveChangesAsync();

                var response = SmsService.SendSingleMessage(vendor.PhoneNumber, "Agency created. Domain : " + vendor.Email);

                notifyService.Custom($"Agency created successfully.", 3, "green", "fas fa-building");
                return RedirectToAction(nameof(VendorsController.Agencies));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        //[Breadcrumb(" Edit Agency", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                if (1 > id || _context.Vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await _context.Vendor.FindAsync(id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendor.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendor.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendor.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", vendor.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", vendor.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendor.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", vendor.PinCodeId);


                var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manager Agency(s)");
                var agency2Page = new MvcBreadcrumbNode("Agencies", "Vendors", "All Agencies") { Parent = agencysPage, };
                var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit Agency") { Parent = agencyPage } ;
                ViewData["BreadcrumbNode"] = editPage;

                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor vendor)
        {
            if (vendorId != vendor.VendorId || vendor is null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
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
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

                _context.Vendor.Update(vendor);

                var response = SmsService.SendSingleMessage(vendor.PhoneNumber, "Agency edited. Domain : " + vendor.Email);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
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

                var vendor = await _context.Vendor
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .FirstOrDefaultAsync(m => m.VendorId == id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manager Agency(s)");
                var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "All Agencies") { Parent = agencysPage, };
                var editPage = new MvcBreadcrumbNode("Delete", "Vendors", $"Delete Agency") { Parent = agencyPage, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;
                return View(vendor);
            }
            catch (Exception ex)
            {
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
                if (_context.Vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = await _context.Vendor.FindAsync(VendorId);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Vendor.Remove(vendor);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Agency deleted successfully.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Agencies));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}