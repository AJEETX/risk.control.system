using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
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

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CompanyController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IHttpClientService httpClientService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;
        private readonly IToastNotification toastNotification;

        public CompanyController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            IHttpClientService httpClientService,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ISmsService SmsService,
            IToastNotification toastNotification)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.userManager = userManager;
            this.httpClientService = httpClientService;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            smsService = SmsService;
            this.toastNotification = toastNotification;
            UserList = new List<UsersViewModel>();
        }

        [Breadcrumb("Manage Company")]
        public IActionResult Index()
        {
            return RedirectToAction("CompanyProfile");
        }

        [Breadcrumb("Company Profile", FromAction = "Index")]
        public async Task<IActionResult> CompanyProfile()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(clientCompany);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit Company", FromAction = "CompanyProfile")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var clientCompany = await _context.ClientCompany
                    .Include(c => c.Country)
                    .Include(c => c.District)
                    .Include(c => c.PinCode)
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var country = _context.Country.OrderBy(o => o.Name);
                var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompany.CountryId).OrderBy(d => d.Name);
                var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompany.StateId).OrderBy(d => d.Name);
                var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompany.DistrictId).OrderBy(d => d.Name);

                ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompany.CountryId);
                ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompany.StateId);
                ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompany.DistrictId);
                ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompany.PinCodeId);
                return View(clientCompany);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        // POST: ClientCompanies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCompany clientCompany)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (clientCompany.ClientCompanyId < 1)
                {
                    notifyService.Error("OOPs !!!..Company Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var existCompany = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
                if (companyDocument is not null)
                {
                    string newFileName = clientCompany.Email + Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(Path.GetFileName(companyDocument.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    companyDocument.CopyTo(new FileStream(upload, FileMode.Create));
                    existCompany.DocumentUrl = "/company/" + newFileName;
                    using var dataStream = new MemoryStream();
                    companyDocument.CopyTo(dataStream);
                    existCompany.DocumentImage = dataStream.ToArray();
                }
                existCompany.CountryId = clientCompany.CountryId;
                existCompany.StateId = clientCompany.StateId;
                existCompany.DistrictId = clientCompany.DistrictId;
                existCompany.PinCodeId = clientCompany.PinCodeId;
                existCompany.Name = clientCompany.Name;
                existCompany.Code = clientCompany.Code;
                existCompany.PhoneNumber = clientCompany.PhoneNumber;
                existCompany.Branch = clientCompany.Branch;
                existCompany.BankName = clientCompany.BankName;
                existCompany.BankAccountNumber = clientCompany.BankAccountNumber;
                existCompany.IFSCCode = clientCompany.IFSCCode;
                existCompany.Addressline = clientCompany.Addressline;
                existCompany.Description = clientCompany.Description;

                existCompany.Updated = DateTime.Now;
                existCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(existCompany);
                await _context.SaveChangesAsync();

                await smsService.DoSendSmsAsync(clientCompany.PhoneNumber, "Company edited. Domain : " + clientCompany.Email);
            }
            
            catch (Exception ex)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Company {clientCompany.Email} edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(CompanyController.CompanyProfile), "Company");
        }

        [Breadcrumb("Manage Users ")]
        public IActionResult Users()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [Breadcrumb("Add User")]
        public IActionResult CreateUser()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser is null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = new ClientCompanyApplicationUser { ClientCompany = company };
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(ClientCompanyApplicationUser user, string emailSuffix)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userFullEmail = user.Email.Trim().ToLower() + "@" + emailSuffix;
                if (user.ProfileImage != null && user.ProfileImage.Length > 0)
                {
                    string newFileName = userFullEmail;
                    string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
                    newFileName += fileExtension;
                    string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", newFileName);
                    user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));

                    using var dataStream = new MemoryStream();
                    user.ProfileImage.CopyTo(dataStream);
                    user.ProfilePicture = dataStream.ToArray();

                    user.ProfilePictureUrl = "/company/" + newFileName;
                }
                //DEMO
                user.Password = Applicationsettings.Password;
                user.Email = userFullEmail;
                user.EmailConfirmed = true;
                user.UserName = userFullEmail;
                user.Mailbox = new Mailbox { Name = userFullEmail };
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.IsClientAdmin = user.UserRole == CompanyRole.COMPANY_ADMIN;
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
                            notifyService.Custom($"User {createdUser.Email} created and locked.", 3, "green", "fas fa-user-lock");
                            await smsService.DoSendSmsAsync(createdUser.PhoneNumber, "User created and locked. Email : " + createdUser.Email);
                            return RedirectToAction(nameof(CompanyController.Users), "Company");
                        }
                    }
                    else
                    {
                        notifyService.Custom($"User {user.Email} created successfully.", 3, "green", "fas fa-user-plus");
                        await smsService.DoSendSmsAsync(user.PhoneNumber, "User created . Email : " + user.Email);
                        return RedirectToAction(nameof(CompanyController.Users), "Company");
                    }
                    notifyService.Custom($"User {user.Email} created successfully.", 3, "green", "fas fa-user-plus");
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Edit User", FromAction = "Users")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (userId == null || _context.ClientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.FindAsync(userId);
                if (clientCompanyApplicationUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

                ViewBag.Show = clientCompanyApplicationUser.Email == companyUser.Email ? false : true;
                var clientCompany = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId);

                if (clientCompany == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ClientCompanyApplicationUser applicationUser)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id != applicationUser.Id.ToString())
                {
                    notifyService.Error("USER NOT FOUND!");
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                var user = await userManager.FindByIdAsync(id);
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    string newFileName = applicationUser.Email + Guid.NewGuid().ToString();
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
                }

                if (user != null)
                {
                    user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                    user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                    user.FirstName = applicationUser?.FirstName;
                    user.LastName = applicationUser?.LastName;
                    if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                    {
                        user.Password = applicationUser.Password;
                    }
                    user.Active = applicationUser.Active;
                    user.Addressline = applicationUser.Addressline;
                    user.Country = applicationUser.Country;
                    user.CountryId = applicationUser.CountryId;
                    user.State = applicationUser.State;
                    user.StateId = applicationUser.StateId;
                    user.PinCode = applicationUser.PinCode;
                    user.PinCodeId = applicationUser.PinCodeId;
                    user.Updated = DateTime.Now;
                    user.Comments = applicationUser.Comments;
                    user.PhoneNumber = applicationUser.PhoneNumber;
                    user.UserRole = applicationUser.UserRole;
                    user.Role = applicationUser.Role != null ? applicationUser.Role : (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                    user.IsClientAdmin = user.UserRole == CompanyRole.COMPANY_ADMIN;
                    user.UpdatedBy = HttpContext.User?.Identity?.Name;
                    user.SecurityStamp = DateTime.Now.ToString();
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
                                notifyService.Custom($"User {createdUser.Email} edited and locked.", 3, "orange", "fas fa-user-lock");
                                await smsService.DoSendSmsAsync(createdUser.PhoneNumber, "User created and locked. Email : " + createdUser.Email);
                                return RedirectToAction(nameof(CompanyController.Users), "Company");
                            }
                        }
                        else
                        {
                            var createdUser = await userManager.FindByEmailAsync(user.Email);
                            var lockUser = await userManager.SetLockoutEnabledAsync(createdUser, true);
                            var lockDate = await userManager.SetLockoutEndDateAsync(user, DateTime.Now);

                            if (lockUser.Succeeded && lockDate.Succeeded)
                            {
                                notifyService.Custom($"User {createdUser.Email} edited and unlocked.", 3, "orange", "fas fa-user-check");
                                await smsService.DoSendSmsAsync(user.PhoneNumber, "User created . Email : " + user.Email);
                                return RedirectToAction(nameof(CompanyController.Users), "Company");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error($"Error to create Company user.", 3);
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }

            notifyService.Error($"Error to create Company user.", 3);
            return RedirectToAction(nameof(CompanyController.Users), "Company");
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
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model =await _context.ClientCompanyApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == userId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Claim Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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
                var model =await _context.ClientCompanyApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode)
                    .FirstOrDefaultAsync(c => c.Email == email);
                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                model.Updated = DateTime.Now;
                model.UpdatedBy = currentUserEmail;
                model.Deleted = true;
                _context.ClientCompanyApplicationUser.Update(model);
                await _context.SaveChangesAsync();
                notifyService.Custom($"User {model.Email} deleted", 3, "red", "fas fa-user-minus");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Available Agencies", FromAction = "Index", FromController = typeof(VendorsController))]
        public IActionResult AvailableVendors()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvailableVendors(List<string> vendors)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (vendors is null || vendors.Count == 0)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var company = _context.ClientCompany.Include(c => c.CompanyApplicationUser).Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendors2Empanel = _context.Vendor.AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                company.EmpanelledVendors.AddRange(vendors2Empanel.ToList());

                company.Updated = DateTime.Now;
                company.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();

                foreach (var vendor2Empanel in vendors2Empanel)
                {
                    vendor2Empanel.Clients.Add(company);
                }

                notifyService.Custom($"Agency(s) empanelled.", 3, "green", "fas fa-thumbs-up");
                return RedirectToAction("AvailableVendors");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Empanelled Agencies", FromAction = "Index", FromController = typeof(VendorsController))]
        public IActionResult EmpanelledVendors()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (currentUserEmail == null)
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpanelledVendors(List<string> vendors)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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
                    .Include(c => c.CompanyApplicationUser)
                    .Include(c => c.EmpanelledVendors)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var empanelledVendors2Depanel = _context.Vendor.Include(v => v.Clients).AsNoTracking().Where(v => vendors.Contains(v.VendorId.ToString()));
                foreach (var empanelledVendor2Depanel in empanelledVendors2Depanel)
                {
                    var empanelled = company.EmpanelledVendors.FirstOrDefault(v => v.VendorId == empanelledVendor2Depanel.VendorId);
                    company.EmpanelledVendors.Remove(empanelled);
                    var clientCompany = empanelledVendor2Depanel.Clients.FirstOrDefault(c => c.ClientCompanyId == company.ClientCompanyId);

                    empanelledVendor2Depanel.Clients.Remove(clientCompany);
                }
                company.Updated = DateTime.Now;
                company.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ClientCompany.Update(company);
                var savedRows = await _context.SaveChangesAsync();
                notifyService.Custom($"Agency(s) de-panelled.", 3, "red", "far fa-thumbs-down");
                return RedirectToAction("EmpanelledVendors");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Detail", FromAction = "AvailableVendors")]
        public async Task<IActionResult> VendorDetail(long id, string backurl)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (1 > id || _context.Vendor == null)
                {
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
                }

                var vendor = await _context.Vendor
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
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
                    notifyService.Error("agency not found!");
                    return RedirectToAction(nameof(CompanyController.Users), "Company");
                }
                ViewBag.Backurl = backurl;

                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Agency Detail", FromAction = "EmpanelledVendors")]
        public async Task<IActionResult> VendorDetails(long id, string backurl)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (1 > id || _context.Vendor == null)
                {
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
                }

                var vendor = await _context.Vendor
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
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
                    notifyService.Error("AGENCY NOT FOUND!");
                    return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
                }
                ViewBag.Backurl = backurl;

                return View(vendor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb("Edit Role", FromAction = "Users")]
        public async Task<IActionResult> UserRoles(string userId)
        {
            var userRoles = new List<CompanyUserRoleViewModel>();
            //ViewBag.userId = userId;
            ClientCompanyApplicationUser user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return NotFound();
            }
            string selectedRole = string.Empty;
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles.Where(r =>
                r.Name.Contains(AppRoles.COMPANY_ADMIN.ToString()) ||
                r.Name.Contains(AppRoles.CREATOR.ToString()) ||
                r.Name.Contains(AppRoles.ASSESSOR.ToString())))
            {
                var userRoleViewModel = new CompanyUserRoleViewModel
                {
                    RoleId = role.Id.ToString(),
                    RoleName = role?.Name
                };
                if (await userManager.IsInRoleAsync(user, role?.Name))
                {
                    userRoleViewModel.Selected = true;
                    selectedRole = role.Name;
                }
                else
                {
                    userRoleViewModel.Selected = false;
                }
                userRoles.Add(userRoleViewModel);
            }
            var model = new CompanyUserRolesViewModel
            {
                UserId = userId,
                CompanyId = user.ClientCompanyId.Value,
                UserName = user.UserName,
                CompanyUserRoleViewModel = userRoles,
                UserRole = !string.IsNullOrWhiteSpace(selectedRole) ? (CompanyRole)Enum.Parse(typeof(CompanyRole), selectedRole, true) : null,
            };

            //var companyPage = new MvcBreadcrumbNode("Index", "Company", "Company");
            //var usersPage = new MvcBreadcrumbNode("User", "Company", "Users") { Parent = companyPage };
            //var userPage = new MvcBreadcrumbNode("EditUser", "Company", $"User") { Parent = usersPage, RouteValues = new { userid = userId } };
            //var userRolePage = new MvcBreadcrumbNode("UserRoles", "Company", $"Edit Role") { Parent = usersPage, RouteValues = new { userid = userId } };
            //ViewData["BreadcrumbNode"] = userRolePage;
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string userId, CompanyUserRolesViewModel model)
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
                toastNotification.AddErrorToastMessage("user not found!");
                return RedirectToAction(nameof(CompanyController.Users), "Company");
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            result = await userManager.AddToRolesAsync(user, new List<string> { model.UserRole.ToString() });
            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            await signInManager.RefreshSignInAsync(currentUser);
            await smsService.DoSendSmsAsync(user.PhoneNumber, "User role edited . Email : " + user.Email);

            notifyService.Custom($"User role(s) updated successfully.", 3, "orange", "fas fa-user-cog");
            return RedirectToAction(nameof(CompanyController.Users));
        }
    }
}