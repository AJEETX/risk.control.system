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

        [Breadcrumb("Agencies", FromAction = "Index")]
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
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
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
                return NotFound();
            }
            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(vendor);
        }

        [Breadcrumb(" Manage Users", FromAction = "Details")]
        public IActionResult Users(string id)
        {
            ViewData["vendorId"] = id;

            return View();
        }

        [Breadcrumb(" Add User", FromAction = "Users")]
        public IActionResult CreateUser(long id)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == id);
            var model = new VendorApplicationUser { Vendor = vendor };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");

            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "All Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            //var usersPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("Create", "VendorApplicationUsers", $"Add User") { Parent = usersPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }

        // POST: VendorApplicationUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser user, string emailSuffix)
        {
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                string newFileName = Guid.NewGuid().ToString();
                string fileExtension = Path.GetExtension(user.ProfileImage.FileName);
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
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            IdentityResult result = await userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
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
                    notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                }
                return RedirectToAction(nameof(Users), "Vendors", new { id = user.VendorId });
            }
            else
            {
                toastNotification.AddErrorToastMessage("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            GetCountryStateEdit(user);
            notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
            return View(user);
        }

        private void GetCountryStateEdit(VendorApplicationUser? user)
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", user?.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", user?.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State.Where(s => s.CountryId == user.CountryId), "StateId", "Name", user?.StateId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode.Where(s => s.StateId == user.StateId), "PinCodeId", "Name", user?.PinCodeId);
        }

        [Breadcrumb(" Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            if (userId == null || _context.VendorApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }

            var vendorApplicationUser = _context.VendorApplicationUser
                .Include(v => v.Mailbox).Where(v => v.Id == userId)
                ?.FirstOrDefault();
            if (vendorApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorApplicationUser.VendorId);

            if (vendor == null)
            {
                toastNotification.AddErrorToastMessage("vendor not found");
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

            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "All Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = vendor.VendorId } };
            //var usersPage = new MvcBreadcrumbNode("Index", "VendorUser", $"Manage Users") { Parent = agencyPage, RouteValues = new { id = vendor.VendorId } };
            //var editPage = new MvcBreadcrumbNode("Edit", "VendorApplicationUsers", $"Edit User") { Parent = usersPage, RouteValues = new { id = userId } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(vendorApplicationUser);
        }

        // POST: VendorApplicationUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser applicationUser)
        {
            if (applicationUser is not null)
            {
                try
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                    {
                        string newFileName = user.Email + Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(applicationUser.ProfileImage.FileName);
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
                        user.Updated = DateTime.UtcNow;
                        user.Comments = applicationUser.Comments;
                        user.PhoneNumber = applicationUser.PhoneNumber;
                        user.UpdatedBy = HttpContext.User?.Identity?.Name;
                        user.SecurityStamp = DateTime.UtcNow.ToString();
                        var result = await userManager.UpdateAsync(user);
                        if (result.Succeeded)
                        {
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

                                if (lockUser.Succeeded && lockDate.Succeeded)
                                {
                                    var response = SmsService.SendSingleMessage(user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
                                    notifyService.Custom($"User edited and unlocked.", 3, "green", "fas fa-user-check");
                                }
                            }
                            return RedirectToAction(nameof(Users), "Vendors", new { id = applicationUser.VendorId });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            toastNotification.AddErrorToastMessage("Error to create agency user!");
            return RedirectToAction(nameof(Index), "Users", new { id = applicationUser.VendorId });
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
                r.Name.Contains(AppRoles.AgencyAdmin.ToString()) ||
                r.Name.Contains(AppRoles.Supervisor.ToString()) ||
                r.Name.Contains(AppRoles.Agent.ToString())))
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
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            var newRoles = model.VendorUserRoleViewModel.Where(x => x.Selected).Select(y => y.RoleName);

            result = await userManager.AddToRolesAsync(user, model.VendorUserRoleViewModel.
                Where(x => x.Selected).Select(y => y.RoleName));
            var isAgent = newRoles.Any(r => AppConstant.AppRoles.Agent.ToString().Contains(r)) && string.IsNullOrWhiteSpace(user.MobileUId);
            System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + Applicationsettings.APP_URL);
            System.Net.WebClient client = new System.Net.WebClient();
            string tinyUrl = client.DownloadString(address);
            var response = SmsService.SendSingleMessage(user.PhoneNumber, "User update. Email : " + user.Email, isAgent);

            if (isAgent)
            {
                var onboard = SmsService.SendSingleMessage(user.PhoneNumber, tinyUrl, isAgent);
            }
            notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            return RedirectToAction("Users", "Vendors", new { id = model.VendorId });
        }

        [Breadcrumb("Manage Service", FromAction = "Details")]
        public IActionResult Service(string id)
        {
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }
            ViewData["vendorId"] = id;
            return View();
        }

        // GET: Vendors/Create
        [Breadcrumb(" Add Agency")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        // POST: Vendors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor, string domainAddress, string mailAddress)
        {
            try
            {
                if (vendor is not null)
                {
                    Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

                    vendor.Email = mailAddress.ToLower() + domainData.GetEnumDisplayName();

                    IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                    if (vendorDocument is not null)
                    {
                        string newFileName = vendor.Email + Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(vendorDocument.FileName);
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
                    vendor.ActivatedDate = DateTime.UtcNow;
                    vendor.DomainName = domainData;
                    vendor.Updated = DateTime.UtcNow;
                    vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

                    _context.Add(vendor);
                    await _context.SaveChangesAsync();

                    var response = SmsService.SendSingleMessage(vendor.PhoneNumber, "Agency created. Domain : " + vendor.Email);

                    notifyService.Custom($"Agency created successfully.", 3, "green", "fas fa-building");
                    return RedirectToAction(nameof(Index));
                }
                notifyService.Error($"Error to create agency!.", 3);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Breadcrumb(" Edit Agency", FromAction = "Details")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor.FindAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }

            var country = _context.Country.OrderBy(o => o.Name);
            var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == vendor.CountryId).OrderBy(d => d.Name);
            var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == vendor.StateId).OrderBy(d => d.Name);
            var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == vendor.DistrictId).OrderBy(d => d.Name);

            ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", vendor.CountryId);
            ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", vendor.StateId);
            ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", vendor.DistrictId);
            ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", vendor.PinCodeId);

            //var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "All Agencies");
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("Edit", "Vendors", $"Edit Agency") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(vendor);
        }

        // POST: Vendors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long vendorId, Vendor vendor)
        {
            if (vendorId != vendor.VendorId)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            if (vendor is not null)
            {
                try
                {
                    IFormFile? vendorDocument = Request.Form?.Files?.FirstOrDefault();
                    if (vendorDocument is not null)
                    {
                        string newFileName = Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(vendorDocument.FileName);
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
                    vendor.Updated = DateTime.UtcNow;
                    vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

                    _context.Vendor.Update(vendor);

                    var response = SmsService.SendSingleMessage(vendor.PhoneNumber, "Agency edited. Domain : " + vendor.Email);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorExists(vendor.VendorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                notifyService.Custom($"Agency edited successfully.", 3, "orange", "fas fa-building");
                return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
            }
            notifyService.Error($"Err Company delete.", 3);
            return RedirectToAction(nameof(VendorsController.Details), "Vendors", new { id = vendorId });
        }

        // GET: Vendors/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == null || _context.Vendor == null)
            {
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }
            var agencysPage = new MvcBreadcrumbNode("Index", "Vendors", "Agencies");
            var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Agency") { Parent = agencysPage, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Delete", "Vendors", $"Delete Agency") { Parent = agencyPage, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = editPage;
            return View(vendor);
        }

        // POST: Vendors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long VendorId)
        {
            if (_context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return Problem("Entity set 'ApplicationDbContext.Vendor'  is null.");
            }
            var vendor = await _context.Vendor.FindAsync(VendorId);
            if (vendor != null)
            {
                vendor.Updated = DateTime.UtcNow;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.Vendor.Remove(vendor);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Agency deleted successfully.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Index));
            }
            notifyService.Error($"Err Agency delete.", 3);
            return RedirectToAction(nameof(Index));
        }

        private bool VendorExists(long id)
        {
            return (_context.Vendor?.Any(e => e.VendorId == id)).GetValueOrDefault();
        }
    }
}