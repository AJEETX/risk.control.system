using Amazon.Rekognition.Model;

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
using SmartBreadcrumbs.Nodes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class CompanyController : Controller
    {
        private const string vendorMapSize = "800x800";
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly INotyfService notifyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly UserManager<VendorApplicationUser> userAgencyManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ISmsService smsService;
        private readonly IToastNotification toastNotification;

        public CompanyController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            UserManager<VendorApplicationUser> userAgencyManager,
            SignInManager<ApplicationUser> signInManager,
            INotyfService notifyService,
            ICustomApiCLient customApiCLient,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ISmsService SmsService,
            IToastNotification toastNotification)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.notifyService = notifyService;
            this.customApiCLient = customApiCLient;
            this.userManager = userManager;
            this.userAgencyManager = userAgencyManager;
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
                //var country = _context.Country.OrderBy(o => o.Name);
                //var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompany.CountryId).OrderBy(d => d.Name);
                //var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompany.StateId).OrderBy(d => d.Name);
                //var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompany.DistrictId).OrderBy(d => d.Name);

                //ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompany.CountryId);
                //ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompany.StateId);
                //ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompany.DistrictId);
                //ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompany.PinCodeId);
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
                if(clientCompany is null || clientCompany.SelectedCountryId < 1 || clientCompany.SelectedStateId < 1 || clientCompany.SelectedDistrictId < 1 || clientCompany.SelectedPincodeId < 1)
                {
                    notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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

                existCompany.PinCodeId = clientCompany.SelectedPincodeId;
                existCompany.DistrictId = clientCompany.SelectedDistrictId;
                existCompany.StateId = clientCompany.SelectedStateId;
                existCompany.CountryId = clientCompany.SelectedCountryId;

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
                //ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
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
            if(user is null || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Company");
            }
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

                user.CountryId = user.SelectedCountryId;
                user.StateId = user.SelectedStateId;
                user.DistrictId = user.SelectedDistrictId;
                user.PinCodeId = user.SelectedPincodeId;

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

                //var country = _context.Country.OrderBy(o => o.Name);
                //var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == clientCompanyApplicationUser.CountryId).OrderBy(d => d.Name);
                //var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == clientCompanyApplicationUser.StateId).OrderBy(d => d.Name);
                //var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == clientCompanyApplicationUser.DistrictId).OrderBy(d => d.Name);

                //ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", clientCompanyApplicationUser.CountryId);
                //ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", clientCompanyApplicationUser.StateId);
                //ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", clientCompanyApplicationUser.DistrictId);
                //ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", clientCompanyApplicationUser.PinCodeId);

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
            if (applicationUser is null || applicationUser.SelectedCountryId < 1 || applicationUser.SelectedStateId < 1 || applicationUser.SelectedDistrictId < 1 || applicationUser.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(EditUser), "Company",new {userid = id });
            }
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

                    user.CountryId = applicationUser.SelectedCountryId;
                    user.StateId = applicationUser.SelectedStateId;
                    user.DistrictId = applicationUser.SelectedDistrictId;
                    user.PinCodeId = applicationUser.SelectedPincodeId;

                    user.IsUpdated = true;
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

        [Breadcrumb("Manage Agency(s)")]
        public IActionResult Agencies()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            return View();
        }
        [Breadcrumb("Available Agencies", FromAction = "Agencies")]
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

        // GET: Vendors/Details/5
        [Breadcrumb(" Manage Agency", FromAction = "EmpanelledVendors")]
        public async Task<IActionResult> AgencyDetail(long id)
        {
            try
            {
                if (id < 1 || _context.Vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
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
                var superAdminUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);

                if (superAdminUser.IsSuperAdmin)
                {
                    vendor.SelectedByCompany = true;
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
        [Breadcrumb(" Edit Agency", FromAction = "AgencyDetail")]
        public async Task<IActionResult> EditAgency(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (1 > id || _context.Vendor == null)
                {
                    notifyService.Error("OOPS !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendor = await _context.Vendor.FindAsync(id);
                if (vendor == null)
                {
                    notifyService.Error("OOPS !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                //var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
                //var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
                //var agencyPage = new MvcBreadcrumbNode("AgencyDetail", "Company", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
                //var editPage = new MvcBreadcrumbNode("EditAgency", "Company", $"Edit Agency") { Parent = agencyPage };
                //ViewData["BreadcrumbNode"] = editPage;

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
        public async Task<IActionResult> EditAgency(long vendorId, Vendor vendor)
        {
            if (vendor is null || vendorId != vendor.VendorId || vendor.SelectedCountryId < 1 || vendor.SelectedStateId < 1 || vendor.SelectedDistrictId < 1 || vendor.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(Edit), "Vendors", new { id = vendorId });
            }

            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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
                vendor.IsUpdated = true;
                vendor.Updated = DateTime.Now;
                vendor.UpdatedBy = HttpContext.User?.Identity?.Name;

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

                await smsService.DoSendSmsAsync(vendor.PhoneNumber, "Agency edited. Domain : " + vendor.Email);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            notifyService.Custom($"Agency edited successfully.", 3, "orange", "fas fa-building");
            return RedirectToAction(nameof(AgencyDetail), "Company", new { id = vendorId });
        }

        [Breadcrumb(" Users", FromAction = "AgencyDetail")]
        public IActionResult AgencyUsers(string id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewData["vendorId"] = id;

            //var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
            //var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
            //var agencyPage = new MvcBreadcrumbNode("AgencyDetail", "Company", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View();
        }
        [Breadcrumb(" Add User", FromAction = "AgencyUsers")]
        public IActionResult CreateAgencyUser(long id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            if (id < 1 || _context.Vendor == null)
            {
                notifyService.Error("OOPS !!!..Error creating user");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var allRoles = Enum.GetValues(typeof(AgencyRole)).Cast<AgencyRole>()?.ToList();

            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == id);
            if (vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentVendorUserCount = _context.VendorApplicationUser.Count(v => v.VendorId == id);
            if (currentVendorUserCount == 0)
            {
                allRoles = allRoles.Where(r => r == AgencyRole.AGENCY_ADMIN).ToList();
            }
            else
            {
                allRoles = allRoles.Where(r => r != AgencyRole.AGENCY_ADMIN).ToList();
            }
            var model = new VendorApplicationUser { Vendor = vendor, AgencyRole = allRoles };

            //var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
            //var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
            //var agencyPage = new MvcBreadcrumbNode("AgencyDetail", "Company", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
            //var usersPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = id } };
            //var editPage = new MvcBreadcrumbNode("CreateAgencyUser", "Company", $"Add User") { Parent = usersPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = editPage;

            return View(model);
        }
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAgencyUser(VendorApplicationUser user, string emailSuffix)
        {
            if (user is null || user.SelectedCountryId < 1 || user.SelectedStateId < 1 || user.SelectedDistrictId < 1 || user.SelectedPincodeId < 1)
            {
                notifyService.Custom($"OOPs !!!..Invalid Data.", 3, "red", "fas fa-building");
                return RedirectToAction(nameof(CreateUser), "Vendors", new { userid = user.Id });
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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

                user.PinCodeId = user.SelectedPincodeId;
                user.DistrictId = user.SelectedDistrictId;
                user.StateId = user.SelectedStateId;
                user.CountryId = user.SelectedCountryId;

                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), user.UserRole.ToString());
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
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
                IdentityResult result = await userAgencyManager.CreateAsync(user, user.Password);

                if (result.Succeeded)
                {
                    var roleResult = await userAgencyManager.AddToRolesAsync(user, new List<string> { user.UserRole.ToString() });
                    var roles = await userAgencyManager.GetRolesAsync(user);

                    if (!user.Active)
                    {
                        var createdUser = await userAgencyManager.FindByEmailAsync(user.Email);
                        var lockUser = await userAgencyManager.SetLockoutEnabledAsync(createdUser, true);
                        var lockDate = await userAgencyManager.SetLockoutEndDateAsync(createdUser, DateTime.MaxValue);

                        if (lockUser.Succeeded && lockDate.Succeeded)
                        {
                            await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user created and locked. Email : " + user.Email);
                            notifyService.Custom($"User edited and locked.", 3, "orange", "fas fa-user-lock");
                        }
                    }
                    else
                    {

                        await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user created. Email : " + user.Email);

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

                            await smsService.DoSendSmsAsync(user.PhoneNumber, message, true);
                            notifyService.Custom($"Agent onboarding initiated.", 3, "green", "fas fa-user-check");
                        }
                        else
                        {
                            await smsService.DoSendSmsAsync(user.PhoneNumber, "Agency user edited and unlocked. Email : " + user.Email);
                        }
                        notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                    }
                    return RedirectToAction(nameof(AgencyDetail), "Company", new { id = user.VendorId });
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

        [Breadcrumb(" Edit User", FromAction = "AgencyUsers")]
        public IActionResult EditAgencyUser(long? userId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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

                //var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
                //var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
                //var agencyPage = new MvcBreadcrumbNode("AgencyDetail", "Company", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = vendor.VendorId } };
                //var usersPage = new MvcBreadcrumbNode("AgencyUsers", "Company", $"Manager Users") { Parent = agencyPage, RouteValues = new { id = vendor.VendorId } };
                //var editPage = new MvcBreadcrumbNode("EditAgencyUser", "Company", $"Edit User") { Parent = usersPage, RouteValues = new { id = userId } };
                //ViewData["BreadcrumbNode"] = editPage;

                return View(vendorApplicationUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Delete", FromAction = "AgencyUsers")]
        public async Task<IActionResult> DeleteAgencyUser(long userId)
        {
            try
            {
                if (userId < 1 || userId == 0)
                {
                    notifyService.Error("OOPS!!!.Id Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await _context.VendorApplicationUser.Include(v => v.Country).Include(v => v.State).Include(v => v.District).Include(v => v.PinCode).FirstOrDefaultAsync(c => c.Id == userId);
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
        [HttpPost, ActionName("DeleteAgencyUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAgencyUser(string email, long vendorId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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
                return RedirectToAction(nameof(AgencyUsers), "Company", new { id = vendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [Breadcrumb("Manage Service", FromAction = "AgencyDetail")]
        public IActionResult Service(string id)
        {
            if (id == null || _context.Vendor == null)
            {
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Unauthenticated Access");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewData["vendorId"] = id;

            //var agencysPage = new MvcBreadcrumbNode("AvailableVendors", "Company", "Manager Agency(s)");
            //var agency2Page = new MvcBreadcrumbNode("AvailableVendors", "Company", "Available Agencies") { Parent = agencysPage, };
            //var agencyPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agency2Page, RouteValues = new { id = id } };
            //var servicesPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manager Service") { Parent = agencyPage, RouteValues = new { id = id } };
            //ViewData["BreadcrumbNode"] = servicesPage;

            return View();
        }
        [Breadcrumb(" Add", FromAction = "Service")]
        public IActionResult CreateService(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == id);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                //ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                var model = new VendorInvestigationServiceType { SelectedMultiPincodeId = new List<long>(), Vendor = vendor, PincodeServices = new List<ServicedPinCode>() };

                //var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manage Agency(s)");
                //var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Agencies") { Parent = agencysPage };
                //var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = id } };
                //var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = id } };
                //var createPage = new MvcBreadcrumbNode("Create", "VendorService", $"Add Service") { Parent = editPage, RouteValues = new { id = id } };
                //ViewData["BreadcrumbNode"] = createPage;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        // POST: VendorService/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType vendorInvestigationServiceType, long VendorId)
        {
            if (vendorInvestigationServiceType is null || vendorInvestigationServiceType.SelectedCountryId < 1 || vendorInvestigationServiceType.SelectedStateId < 1 || vendorInvestigationServiceType.SelectedDistrictId < 1)
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(CreateService), "Agency", new { id = VendorId });
            }
            try
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
                vendorInvestigationServiceType.CountryId = vendorInvestigationServiceType.SelectedCountryId;
                vendorInvestigationServiceType.StateId = vendorInvestigationServiceType.SelectedStateId;
                vendorInvestigationServiceType.DistrictId = vendorInvestigationServiceType.SelectedDistrictId;

                _context.Add(vendorInvestigationServiceType);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Service created successfully.", 3, "green", "fas fa-truck");

                return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = vendorInvestigationServiceType.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Edit", FromAction = "Service")]
        public async Task<IActionResult> EditService(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Error("OOPs !!!..Agency Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType == null)
                {
                    return NotFound();
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

                ViewBag.PinCodeId = _context.PinCode.Include(p => p.District).Where(p => p.District.DistrictId == vendorInvestigationServiceType.DistrictId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name + " - " + x.Code,
                        Value = x.PinCodeId.ToString()
                    }).ToList();

                var selected = services.PincodeServices.Select(s => s.Pincode).ToList();
                services.SelectedMultiPincodeId = _context.PinCode.Where(p => selected.Contains(p.Code)).Select(p => p.PinCodeId).ToList();

                //var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manage Agency(s)");
                //var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Agencies") { Parent = agencysPage };
                //var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = services.VendorId } };
                //var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = services.VendorId } };
                //var createPage = new MvcBreadcrumbNode("Edit", "VendorService", $"Edit Service") { Parent = editPage, RouteValues = new { id = id } };
                //ViewData["BreadcrumbNode"] = createPage;

                return View(services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(long VendorInvestigationServiceTypeId, VendorInvestigationServiceType vendorInvestigationServiceType)
        {
            if (vendorInvestigationServiceType is null || vendorInvestigationServiceType.SelectedCountryId < 1 || vendorInvestigationServiceType.SelectedStateId < 1 || vendorInvestigationServiceType.SelectedDistrictId < 1)
            {
                notifyService.Custom($"Error to edit service.", 3, "red", "fas fa-truck");
                return RedirectToAction(nameof(EditService), "VendorService", new { id = VendorInvestigationServiceTypeId });
            }
            try
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
                vendorInvestigationServiceType.IsUpdated = true;
                vendorInvestigationServiceType.CountryId = vendorInvestigationServiceType.SelectedCountryId;
                vendorInvestigationServiceType.StateId = vendorInvestigationServiceType.SelectedStateId;
                vendorInvestigationServiceType.DistrictId = vendorInvestigationServiceType.SelectedDistrictId;

                _context.Update(vendorInvestigationServiceType);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Service updated successfully.", 3, "orange", "fas fa-truck");
                return RedirectToAction(nameof(CompanyController.Service), "Company", new { id = vendorInvestigationServiceType.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // GET: VendorService/Delete/5
        [Breadcrumb(" Delete", FromAction = "Service")]
        public async Task<IActionResult> DeleteService(long id)
        {
            try
            {
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
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
                    return NotFound();
                }
                //var agencysPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Manage Agency(s)");
                //var agencyPage = new MvcBreadcrumbNode("Agencies", "Vendors", "Agencies") { Parent = agencysPage };
                //var agencyDetailPage = new MvcBreadcrumbNode("Details", "Vendors", "Manage Agency") { Parent = agencyPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                //var editPage = new MvcBreadcrumbNode("Service", "Vendors", $"Manage Service") { Parent = agencyDetailPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                //var createPage = new MvcBreadcrumbNode("Delete", "VendorService", $"Delete Service") { Parent = editPage, RouteValues = new { id = vendorInvestigationServiceType.VendorId } };
                //ViewData["BreadcrumbNode"] = createPage;

                return View(vendorInvestigationServiceType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        // POST: VendorService/Delete/5
        [HttpPost, ActionName("DeleteService")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                if (id == 0 || _context.VendorInvestigationServiceType == null)
                {
                    notifyService.Error("OOPs !!!..Id Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
                if (vendorInvestigationServiceType != null)
                {
                    vendorInvestigationServiceType.Updated = DateTime.Now;
                    vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.VendorInvestigationServiceType.Remove(vendorInvestigationServiceType);
                    await _context.SaveChangesAsync();
                    notifyService.Custom($"Service deleted successfully.", 3, "red", "fas fa-truck");
                    return RedirectToAction("Service", "Company", new { id = vendorInvestigationServiceType.VendorId });
                }
                notifyService.Error($"Err Service delete.", 3);
                return RedirectToAction("Service", "Company", new { id = vendorInvestigationServiceType.VendorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
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