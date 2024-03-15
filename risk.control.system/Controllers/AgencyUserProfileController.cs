using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;

using risk.control.system.Models;
using SmartBreadcrumbs.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using risk.control.system.Services;
using AspNetCoreHero.ToastNotification.Abstractions;

namespace risk.control.system.Controllers
{
    [Breadcrumb("User Profile ")]
    public class AgencyUserProfileController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgencyUserProfileController(ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager,
            INotyfService notifyService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            this.toastNotification = toastNotification;
            this.webHostEnvironment = webHostEnvironment;
            UserList = new List<UsersViewModel>();
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser
                .Include(u => u.PinCode)
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .FirstOrDefault(c => c.Email == userEmail);

            return View(vendorUser);
        }

        [Breadcrumb("Edit Profile")]
        public async Task<IActionResult> Edit(long? userId)
        {
            if (userId == null || _context.VendorApplicationUser == null)
            {
                notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                toastNotification.AddErrorToastMessage("agency not found");
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser.FindAsync(userId);
            if (vendorApplicationUser == null)
            {
                notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
                return NotFound();
            }
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorApplicationUser.VendorId);

            if (vendor == null)
            {
                notifyService.Custom($"No user not found.", 3, "red", "fas fa-user");
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

            return View(vendorApplicationUser);
        }

        // POST: ClientCompanyApplicationUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VendorApplicationUser applicationUser)
        {
            if (id != applicationUser.Id.ToString())
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }
            try
            {
                var user = await userManager.FindByIdAsync(id);
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    string newFileName = applicationUser.Email + Guid.NewGuid().ToString();
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
                    user.Email = applicationUser.Email;
                    user.UserName = applicationUser.Email;
                    user.EmailConfirmed = true;
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
                        notifyService.Custom($"User profile edited successfully.", 3, "green", "fas fa-user");

                        var response = SmsService.SendSingleMessage(user.PhoneNumber, "Agency user edited. Email : " + user.Email);

                        return RedirectToAction(nameof(Index), "Dashboard");
                    }
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

            notifyService.Custom($"Error to create edit user.", 3, "red", "fas fa-user");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [HttpGet]
        [Breadcrumb("Change Password ")]
        public IActionResult ChangePassword()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (vendorUser != null)
            {
                return View();
            }
            toastNotification.AddErrorToastMessage("Error to create Agency user!");
            return RedirectToAction(nameof(Index), "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("/Account/Login");
                }

                var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View();
                }

                await signInManager.RefreshSignInAsync(user);
                return View("ChangePasswordConfirmation");
            }

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Password Change Succees")]
        public IActionResult ChangePasswordConfirmation()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (vendorUser != null)
            {
                notifyService.Custom($"Password edited successfully.", 3, "orange", "fas fa-user");
                return View();
            }
            toastNotification.AddErrorToastMessage("Error to create Agency user!");
            return RedirectToAction(nameof(Index), "Dashboard");
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
    }
}