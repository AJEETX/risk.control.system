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
    [Breadcrumb("Agency ")]
    public class AgencyController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgencyController(ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.toastNotification = toastNotification;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

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
                .FirstOrDefaultAsync(m => m.VendorId == vendorUser.VendorId);
            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // GET: Vendors/Edit/5
        [Breadcrumb(" Edit Profile")]
        public async Task<IActionResult> Edit()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var vendor = await _context.Vendor.FindAsync(vendorUser.VendorId);
            if (vendor == null)
            {
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", vendor.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name");
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", vendor.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendor.StateId);
            return View(vendor);
        }

        // POST: Vendors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor vendor)
        {
            if (vendor == null || string.IsNullOrWhiteSpace(vendor.VendorId))
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

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
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "upload", newFileName);
                        vendor.Document = vendorDocument;

                        using var dataStream = new MemoryStream();
                        await vendor.Document.CopyToAsync(dataStream);
                        vendor.DocumentImage = dataStream.ToArray();
                        vendor.DocumentUrl = newFileName;
                    }
                    else
                    {
                        var existingVendor = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                        if (existingVendor.DocumentImage != null)
                        {
                            vendor.DocumentImage = existingVendor.DocumentImage;
                        }
                    }
                    vendor.Updated = DateTime.UtcNow;
                    vendor.UpdatedBy = HttpContext.User?.Identity?.Name;
                    _context.Vendor.Update(vendor);
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
                toastNotification.AddSuccessToastMessage("agency edited successfully!");
                return RedirectToAction(nameof(AgencyController.Index), "Agency");
            }
            return Problem();
        }

        [Breadcrumb("Users ")]
        public async Task<IActionResult> User()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var vendor = _context.Vendor
                .Include(c => c.VendorApplicationUser)
                .FirstOrDefault(c => c.VendorId == vendorUser.VendorId);
            var model = new VendorUsersViewModel
            {
                Vendor = vendor,
            };
            var users = vendor.VendorApplicationUser.AsQueryable();
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
                thisViewModel.VendorName = vendor.Name;
                thisViewModel.VendorId = user.VendorId;
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
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);
            var model = new VendorApplicationUser { Vendor = vendor };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(VendorApplicationUser user)
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
            user.UserName = user.Email;
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
            if (userId == null || _context.VendorApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("agency not found");
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(userId);
            if (vendorApplicationUser == null)
            {
                toastNotification.AddErrorToastMessage("agency not found");
                return NotFound();
            }
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorApplicationUser.VendorId);

            if (vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found");
                return NotFound();
            }
            vendorApplicationUser.Vendor = vendor;
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", vendor.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name");
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", vendor.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendor.StateId);
            return View(vendorApplicationUser);
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, VendorApplicationUser applicationUser)
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
                        user.UserName = applicationUser.Email;
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
                            toastNotification.AddSuccessToastMessage("Agency user edited successfully!");
                            return RedirectToAction(nameof(AgencyController.User), "Agency");
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

            toastNotification.AddErrorToastMessage("Error to create Agency user!");
            return RedirectToAction(nameof(AgencyController.User), "Agency");
        }

        [Breadcrumb("User Role ")]
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
                r.Name.Contains(AppRoles.VendorAdmin.ToString()) ||
                r.Name.Contains(AppRoles.VendorSupervisor.ToString()) ||
                r.Name.Contains(AppRoles.VendorAgent.ToString())))
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
                VendorId = user.VendorId,
                UserName = user.UserName,
                VendorUserRoleViewModel = userRoles
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

            toastNotification.AddSuccessToastMessage("role(s) updated successfully!");
            return RedirectToAction(nameof(AgencyController.User), "Agency");
        }

        [Breadcrumb(" Service")]
        public async Task<IActionResult> Service()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);

            var applicationDbContext = _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.LineOfBusiness)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                 .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.Country)
                .Include(i => i.District)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.PincodeServices)
                .FirstOrDefault(a => a.VendorId == vendor.VendorId);

            return View(applicationDbContext);
        }

        [Breadcrumb(" CreateService")]
        public IActionResult CreateService()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorUser.VendorId);

            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            var model = new VendorInvestigationServiceType { SelectedMultiPincodeId = new List<string>(), Vendor = vendor, PincodeServices = new List<ServicedPinCode>() };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(VendorInvestigationServiceType vendorInvestigationServiceType)
        {
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
                vendorInvestigationServiceType.Updated = DateTime.UtcNow;
                vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                vendorInvestigationServiceType.Created = DateTime.UtcNow;
                _context.Add(vendorInvestigationServiceType);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("service created successfully!");

                return RedirectToAction(nameof(AgencyController.Service), "Agency");
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", vendorInvestigationServiceType.CountryId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", vendorInvestigationServiceType.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
            toastNotification.AddErrorToastMessage("Error to create vendor service!");

            return View(vendorInvestigationServiceType);
        }

        [Breadcrumb(" EditService")]
        public async Task<IActionResult> EditService(string id)
        {
            if (id == null || _context.VendorInvestigationServiceType == null)
            {
                return NotFound();
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

            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
            ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
            ViewData["DistrictId"] = new SelectList(_context.District.Where(d => d.State.StateId == vendorInvestigationServiceType.StateId), "DistrictId", "Name", vendorInvestigationServiceType.DistrictId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", vendorInvestigationServiceType.CountryId);
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

        // POST: VendorService/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(string id, VendorInvestigationServiceType vendorInvestigationServiceType)
        {
            if (id != vendorInvestigationServiceType.VendorInvestigationServiceTypeId)
            {
                return NotFound();
            }

            if (vendorInvestigationServiceType is not null)
            {
                try
                {
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
                        vendorInvestigationServiceType.Updated = DateTime.UtcNow;
                        vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                        _context.Update(vendorInvestigationServiceType);
                        await _context.SaveChangesAsync();
                        toastNotification.AddSuccessToastMessage("service updated successfully!");
                        return RedirectToAction(nameof(VendorsController.Service), "Vendors", new { id = vendorInvestigationServiceType.VendorId });
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!VendorInvestigationServiceTypeExists(vendorInvestigationServiceType.VendorInvestigationServiceTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("service edited successfully!");
                return RedirectToAction("Details", "Vendors", new { id = vendorInvestigationServiceType.VendorId });
            }
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", vendorInvestigationServiceType.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", vendorInvestigationServiceType.LineOfBusinessId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", vendorInvestigationServiceType.StateId);
            ViewData["VendorId"] = new SelectList(_context.Vendor, "VendorId", "Name", vendorInvestigationServiceType.VendorId);
            return View(vendorInvestigationServiceType);
        }

        // GET: VendorService/Delete/5
        [Breadcrumb(" DeleteService")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.VendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.LineOfBusiness)
                .Include(v => v.PincodeServices)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
            if (vendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            return View(vendorInvestigationServiceType);
        }

        // POST: VendorService/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.VendorInvestigationServiceType == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VendorInvestigationServiceType'  is null.");
            }
            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType.FindAsync(id);
            if (vendorInvestigationServiceType != null)
            {
                vendorInvestigationServiceType.Updated = DateTime.UtcNow;
                vendorInvestigationServiceType.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.VendorInvestigationServiceType.Remove(vendorInvestigationServiceType);
            }

            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("service deleted successfully!");
            return RedirectToAction("Details", "Vendors", new { id = vendorInvestigationServiceType.VendorId });
        }

        [Breadcrumb(" ServiceDetail")]
        public async Task<IActionResult> ServiceDetail(string id)
        {
            if (id == null || _context.VendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            var vendorInvestigationServiceType = await _context.VendorInvestigationServiceType
                .Include(v => v.InvestigationServiceType)
                .Include(v => v.LineOfBusiness)
                .Include(v => v.PincodeServices)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.VendorInvestigationServiceTypeId == id);
            if (vendorInvestigationServiceType == null)
            {
                return NotFound();
            }

            return View(vendorInvestigationServiceType);
        }

        private bool VendorInvestigationServiceTypeExists(string id)
        {
            return (_context.VendorInvestigationServiceType?.Any(e => e.VendorInvestigationServiceTypeId == id)).GetValueOrDefault();
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

        private void GetCountryStateEdit(VendorApplicationUser? user)
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", user?.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", user?.DistrictId);
            ViewData["StateId"] = new SelectList(_context.State.Where(s => s.CountryId == user.CountryId), "StateId", "Name", user?.StateId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode.Where(s => s.StateId == user.StateId), "PinCodeId", "Name", user?.PinCodeId);
        }

        private async Task<List<string>> GetUserRoles(VendorApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }

        private bool VendorExists(string id)
        {
            return (_context.Vendor?.Any(e => e.VendorId == id)).GetValueOrDefault();
        }
    }
}