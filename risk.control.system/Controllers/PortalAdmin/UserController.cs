using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Controllers.Common;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Breadcrumb(" Users")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class UserController(UserManager<ApplicationUser> userManager,

            IFileStorageService fileStorageService,
            INotyfService notifyService,
            IFeatureManager featureManager,
            ISmsService smsService,
            ApplicationDbContext context) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly INotyfService _notifyService = notifyService;
        private readonly ISmsService _smsService = smsService;
        private readonly ApplicationDbContext _context = context;
        private readonly IFeatureManager _featureManager = featureManager;

        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb(" Create")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user)
        {
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                var (fileName, relativePath) = await _fileStorageService.SaveAsync(user.ProfileImage, "admin", "user");
                user.ProfilePictureUrl = relativePath;
                user.ProfilePictureExtension = Path.GetExtension(fileName);
            }
            user.EmailConfirmed = true;
            user.Email = user.Email!.Trim().ToLower();
            user.UserName = user.Email;
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            user.PhoneNumber = user.PhoneNumber!.TrimStart('0');
            user.PinCodeId = user.SelectedPincodeId;
            user.StateId = user.SelectedStateId;
            user.DistrictId = user.SelectedDistrictId;
            user.CountryId = user.SelectedCountryId;
            IdentityResult result = await _userManager.CreateAsync(user, user.Password!);
            if (!result.Succeeded)
            {
                _notifyService.Error("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(user);
            }
            var roles = await _userManager.GetRolesAsync(user);
            var roleResult = await _userManager.RemoveFromRolesAsync(user, roles);
            await _userManager.AddToRoleAsync(user, user.Role.ToString()!);
            _notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
            await _smsService.DoSendSmsAsync(country!.Code, country.ISDCode + user.PhoneNumber, "User created. \nEmail : " + user.Email);
            return RedirectToAction(nameof(Index));
        }

        [Breadcrumb(" Edit")]
        public async Task<IActionResult> Edit(long id)
        {
            if (id < 1)
            {
                return NotFound();
            }
            var applicationUser = await _context.ApplicationUser.AsNoTracking()
                .Include(u => u.Country)
                .FirstOrDefaultAsync(c => c.Id == id);
            applicationUser!.IsPasswordChangeRequired = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !applicationUser.IsPasswordChangeRequired : true;
            return View(applicationUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(string id)
        {
            var user = await _context.ApplicationUser.FirstOrDefaultAsync(a => a.Id.ToString() == id);
            if (user is not null)
            {
                user.Updated = DateTime.UtcNow;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.ProfilePictureUrl = null;
                await _context.SaveChangesAsync();
                _notifyService.Success($"Image deleted successfully.", 3);
                return Ok(new { message = "succes", succeeded = true });
            }
            _notifyService.Error("image not found!");
            return NotFound("failed");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] ApplicationUser applicationUser)
        {
            if (id != applicationUser.Id.ToString())
            {
                _notifyService.Error("user not found!");
                return NotFound();
            }
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _notifyService.Error("user not found!");
                    return NotFound();
                }
                if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                {
                    var (fileName, relativePath) = await _fileStorageService.SaveAsync(user.ProfileImage!, "admin", "user");
                    user.ProfilePictureUrl = relativePath;
                    user.ProfilePictureExtension = Path.GetExtension(fileName);
                }
                UpdateUser(user, applicationUser!);
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _notifyService.Error("user not found!");
                    return NotFound();
                }
                var roles = await _userManager.GetRolesAsync(user);
                var roleResult = await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, user.Role.ToString()!);
                var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
                await _smsService.DoSendSmsAsync(country!.Code, country.ISDCode + user.PhoneNumber, "User edited. \nEmail : " + user.Email);
                _notifyService.Custom($"User edited successfully.", 3, "orange", "fas fa-user-check");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                _notifyService.Error("Error !!. The user con't be edited!");
                return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
            }
        }
        private void UpdateUser(ApplicationUser user, ApplicationUser applicationUser)
        {
            user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
            user.PhoneNumber = user.PhoneNumber!.TrimStart('0');
            user.FirstName = applicationUser?.FirstName!;
            user.LastName = applicationUser?.LastName!;
            user.Country = applicationUser!.Country;
            user.Active = applicationUser.Active;
            user.Addressline = applicationUser.Addressline;
            user.CountryId = applicationUser.SelectedCountryId;
            user.StateId = applicationUser.SelectedStateId;
            user.DistrictId = applicationUser.SelectedDistrictId;
            user.PinCodeId = applicationUser.SelectedPincodeId;
            user.IsUpdated = true;
            user.Updated = DateTime.UtcNow;
            user.PhoneNumber = applicationUser.PhoneNumber;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            user.SecurityStamp = DateTime.UtcNow.ToString();
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id <= 0)
                {
                    _notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(DashboardController.Index), ControllerName<DashboardController>.Name); ;
                }
                var appUser = await _context.ApplicationUser.FindAsync(id);
                _context.ApplicationUser.Remove(appUser!);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User deleted successfully!" });
            }
            catch (Exception)
            {
                _notifyService.Error("Error deleting user. Try again.");
                return Json(new { success = false, message = "Error deleting user. Try again." });
            }
        }
    }
}