using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb("Company ")]
    public class CompanyController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;

        public CompanyController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IToastNotification toastNotification)
        {
            this._context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            this.toastNotification = toastNotification;
            UserList = new List<UsersViewModel>();
        }

        public async Task<IActionResult> Index()
        {
            if (_context.ClientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var clientCompany = await _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
            if (clientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }

            return View(clientCompany);
        }

        [Breadcrumb("Manage ")]
        public async Task<IActionResult> Edit()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var clientCompany = await _context.ClientCompany
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClientCompanyId == companyUser.ClientCompanyId);
            if (clientCompany == null)
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", clientCompany.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", clientCompany.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", clientCompany.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", clientCompany.StateId);
            return View(clientCompany);
        }

        // POST: ClientCompanies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCompany clientCompany)
        {
            if (string.IsNullOrWhiteSpace(clientCompany.ClientCompanyId))
            {
                toastNotification.AddErrorToastMessage("client company not found!");
                return NotFound();
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (clientCompany is not null)
            {
                try
                {
                    IFormFile? companyDocument = Request.Form?.Files?.FirstOrDefault();
                    if (companyDocument is not null)
                    {
                        clientCompany.Document = companyDocument;
                        using var dataStream = new MemoryStream();
                        await clientCompany.Document.CopyToAsync(dataStream);
                        clientCompany.DocumentImage = dataStream.ToArray();
                    }
                    else
                    {
                        var existingClientCompany = await _context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                        if (existingClientCompany.DocumentImage != null)
                        {
                            clientCompany.DocumentImage = existingClientCompany.DocumentImage;
                        }
                    }
                    clientCompany.Updated = DateTime.UtcNow;
                    clientCompany.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.ClientCompany.Update(clientCompany);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientCompanyExists(clientCompany.ClientCompanyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("company Profile edited successfully!");
                return RedirectToAction(nameof(CompanyController.Index), "Company");
            }
            toastNotification.AddErrorToastMessage("Error to edit company profile!");
            return Problem();
        }

        [Breadcrumb("User ")]
        public async Task<IActionResult> User()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var model = new CompanyUsersViewModel
            {
                Company = company,
            };
            var users = company.CompanyApplicationUser.AsQueryable();
            foreach (var user in users)
            {
                var country = _context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                var state = _context.State.FirstOrDefault(c => c.StateId == user.StateId);
                var district = _context.District.FirstOrDefault(c => c.DistrictId == user.DistrictId);
                var pinCode = _context.PinCode.FirstOrDefault(c => c.PinCodeId == user.PinCodeId);

                var thisViewModel = new UsersViewModel();
                thisViewModel.UserId = user.Id.ToString();
                thisViewModel.Email = user?.Email;
                thisViewModel.UserName = user?.UserName;
                thisViewModel.ProfileImage = user?.ProfilePictureUrl ?? Applicationsettings.NO_IMAGE;
                thisViewModel.FirstName = user.FirstName;
                thisViewModel.LastName = user.LastName;
                thisViewModel.Country = country.Name;
                thisViewModel.CountryId = user.CountryId;
                thisViewModel.StateId = user.StateId;
                thisViewModel.State = state.Name;
                thisViewModel.PinCode = pinCode.Name;
                thisViewModel.PinCodeId = pinCode.PinCodeId;
                thisViewModel.CompanyName = company.Name;
                thisViewModel.CompanyId = user.ClientCompanyId;
                thisViewModel.ProfileImageInByte = user.ProfilePicture;
                thisViewModel.Roles = await GetUserRoles(user);
                UserList.Add(thisViewModel);
            }
            model.Users = UserList;
            return View(model);
        }

        [Breadcrumb("CreateUser ")]
        public IActionResult CreateUser()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var company = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == companyUser.ClientCompanyId);
            var model = new ClientCompanyApplicationUser { ClientCompany = company };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCompanyApplicationUser user)
        {
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                string newFileName = Guid.NewGuid().ToString();
                string fileExtension = Path.GetExtension(user.ProfileImage.FileName);
                newFileName += fileExtension;
                var upload = Path.Combine(webHostEnvironment.WebRootPath, "upload", newFileName);
                user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                user.ProfilePictureUrl = "upload/" + newFileName;
            }
            user.EmailConfirmed = true;
            user.Mailbox = new Mailbox { Name = user.Email };
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            IdentityResult result = await userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
                return RedirectToAction(nameof(CompanyController.User), "Company");
            else
            {
                toastNotification.AddErrorToastMessage("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            GetCountryStateEdit(user);
            toastNotification.AddSuccessToastMessage("User created successfully!");
            return View(user);
        }

        [Breadcrumb("EditUser ")]
        public async Task<IActionResult> EditUser(long? userId)
        {
            if (userId == null || _context.ClientCompanyApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("company not found");
                return NotFound();
            }

            var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.FindAsync(userId);
            if (clientCompanyApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("company not found");
                return NotFound();
            }
            var clientComapany = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == clientCompanyApplicationUser.ClientCompanyId);

            if (clientComapany == null)
            {
                toastNotification.AddErrorToastMessage("company not found");
                return NotFound();
            }
            clientCompanyApplicationUser.ClientCompany = clientComapany;
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", clientComapany.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name");
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", clientComapany.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", clientComapany.StateId);
            return View(clientCompanyApplicationUser);
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ClientCompanyApplicationUser applicationUser)
        {
            if (id != applicationUser.Id.ToString())
            {
                toastNotification.AddErrorToastMessage("company not found!");
                return NotFound();
            }

            if (applicationUser is not null)
            {
                try
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                    {
                        string newFileName = Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(applicationUser.ProfileImage.FileName);
                        newFileName += fileExtension;
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "upload", newFileName);
                        applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                        applicationUser.ProfilePictureUrl = "upload/" + newFileName;
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
                        user.Email = applicationUser.Email;
                        user.EmailConfirmed = true;
                        user.UserName = applicationUser.UserName;
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
                            toastNotification.AddSuccessToastMessage("Company user edited successfully!");
                            return RedirectToAction(nameof(CompanyUserController.User), "Company");
                        }
                        toastNotification.AddErrorToastMessage("Error !!. The user con't be edited!");
                        Errors(result);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorApplicationUserExists(applicationUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            toastNotification.AddErrorToastMessage("Error to create Company user!");
            return RedirectToAction(nameof(CompanyUserController.User), "Company", new { id = applicationUser.ClientCompany });
        }

        [HttpGet]
        public async Task<IActionResult> AvailableVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var vendorUsers = _context.VendorApplicationUser.Include(u => u.Vendor).Where(u => u.Vendor.ClientCompanyId == companyUser.ClientCompanyId).ToList();
            List<string> userVendorids = new List<string>();
            if (vendorUsers is not null && vendorUsers.Count > 0)
            {
                userVendorids = vendorUsers.Select(u => u.VendorId).ToList();
            }

            var applicationDbContext = _context.Vendor
                .Where(v => v.ClientCompanyId != companyUser.ClientCompanyId
                //&& userVendorids.Contains(v.VendorId)
                && (v.VendorInvestigationServiceTypes != null) && v.VendorInvestigationServiceTypes.Count > 0)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .AsQueryable();

            ViewBag.CompanyId = companyUser.ClientCompanyId;
            return View(applicationDbContext);
        }

        [HttpPost]
        public async Task<IActionResult> AvailableVendors(string id, List<string> vendors)
        {
            if (vendors is not null && vendors.Count > 0)
            {
                var company = await _context.ClientCompany.FindAsync(id);
                if (company != null)
                {
                    var empanelledVendors = _context.Vendor.Where(v => vendors.Contains(v.VendorId))
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.LineOfBusiness)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.PincodeServices);
                    company.EmpanelledVendors.AddRange(empanelledVendors);
                    company.Updated = DateTime.UtcNow;
                    company.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.ClientCompany.Update(company);
                    var savedRows = await _context.SaveChangesAsync();
                    toastNotification.AddSuccessToastMessage("Vendor(s) empanel successful!");
                    try
                    {
                        return RedirectToAction("AvailableVendors");
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            ViewBag.CompanyId = id;
            return Problem();
        }

        [HttpGet]
        public async Task<IActionResult> EmpanelledVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var applicationDbContext = _context.Vendor
                .Where(v => v.ClientCompanyId == companyUser.ClientCompanyId)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .AsQueryable();
            ViewBag.CompanyId = companyUser.ClientCompanyId;

            return View(applicationDbContext);
        }

        [HttpPost]
        public async Task<IActionResult> EmpanelledVendors(List<string> vendors)
        {
            if (vendors is not null && vendors.Count() > 0)
            {
                var userEmail = HttpContext.User?.Identity?.Name;
                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

                var company = _context.ClientCompany
                    .Include(c => c.CompanyApplicationUser)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                if (company != null)
                {
                    var empanelledVendors = _context.Vendor.Where(v => vendors.Contains(v.VendorId))
                    .Where(v => v.ClientCompanyId == companyUser.ClientCompanyId)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.LineOfBusiness)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.InvestigationServiceType)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.PincodeServices);
                    foreach (var v in empanelledVendors)
                    {
                        company.EmpanelledVendors.Remove(v);
                    }
                    _context.ClientCompany.Update(company);
                    company.Updated = DateTime.UtcNow;
                    company.UpdatedBy = HttpContext.User?.Identity?.Name;
                    var savedRows = await _context.SaveChangesAsync();
                    toastNotification.AddSuccessToastMessage("Vendor(s) depanel sucessful!");
                    try
                    {
                        if (savedRows > 0)
                        {
                            return RedirectToAction("EmpanelledVendors");
                        }
                        else
                        {
                            return Problem();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            return Problem();
        }

        public async Task<IActionResult> VendorDetail(string id, string backurl)
        {
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("vendor not found!");
                return NotFound();
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
                return NotFound();
            }
            ViewBag.Backurl = backurl;

            return View(vendor);
        }

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
            //ViewBag.UserName = user.UserName;
            foreach (var role in roleManager.Roles.Where(r =>
                r.Name.Contains(AppRoles.ClientAdmin.ToString()) ||
                r.Name.Contains(AppRoles.ClientCreator.ToString()) ||
                r.Name.Contains(AppRoles.ClientAssigner.ToString()) ||
                r.Name.Contains(AppRoles.ClientAssessor.ToString())))
            {
                var userRoleViewModel = new CompanyUserRoleViewModel
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
            var model = new CompanyUserRolesViewModel
            {
                UserId = userId,
                CompanyId = user.ClientCompanyId,
                UserName = user.UserName,
                CompanyUserRoleViewModel = userRoles
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(string userId, CompanyUserRolesViewModel model)
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
            result = await userManager.AddToRolesAsync(user, model.CompanyUserRoleViewModel.Where(x => x.Selected).Select(y => y.RoleName));
            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            await signInManager.RefreshSignInAsync(currentUser);

            toastNotification.AddSuccessToastMessage("roles updated successfully!");
            return RedirectToAction(nameof(CompanyController.User), "Company");
        }

        private bool VendorApplicationUserExists(long id)
        {
            return (_context.VendorApplicationUser?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        private async Task<List<string>> GetUserRoles(ClientCompanyApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }

        private bool ClientCompanyExists(string id)
        {
            return (_context.ClientCompany?.Any(e => e.ClientCompanyId == id)).GetValueOrDefault();
        }

        private void GetCountryStateEdit(ClientCompanyApplicationUser? user)
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", user?.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", user?.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State.Where(s => s.CountryId == user.CountryId), "StateId", "Name", user?.StateId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode.Where(s => s.StateId == user.StateId), "PinCodeId", "Name", user?.PinCodeId);
        }
    }
}