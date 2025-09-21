﻿using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Users")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly ISmsService smsService;
        public List<UsersViewModel> UserList;
        private readonly ApplicationDbContext context;
        private readonly IFeatureManager featureManager;

        public UserController(UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IFeatureManager featureManager,
            ISmsService SmsService,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            smsService = SmsService;
            this.featureManager = featureManager;
            this.context = context;
            UserList = new List<UsersViewModel>();
        }

        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb(" Create")]
        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(context.Country, "CountryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user)
        {
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                string newFileName = Guid.NewGuid().ToString();
                string fileExtension = Path.GetExtension(Path.GetFileName(user.ProfileImage.FileName));
                newFileName += fileExtension;
                var upload = Path.Combine(webHostEnvironment.WebRootPath, "img", newFileName);
                user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                user.ProfilePictureUrl = "/img/" + newFileName;
                using var dataStream = new MemoryStream();
                user.ProfileImage.CopyTo(dataStream);
                user.ProfilePicture = dataStream.ToArray();
                user.ProfilePictureExtension = fileExtension;
            }
            user.EmailConfirmed = true;
            user.Email = user.Email.Trim().ToLower();
            user.UserName = user.Email;
            user.Updated = DateTime.Now;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;

            user.PinCodeId = user.SelectedPincodeId;
            user.StateId = user.SelectedStateId;
            user.DistrictId = user.SelectedDistrictId;
            user.CountryId = user.SelectedCountryId;

            IdentityResult result = await userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                var country = context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "User created. \n\nEmail : " + user.Email);

                return RedirectToAction(nameof(Index));
            }
            else
            {
                notifyService.Error("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            return View(user);
        }

        [Breadcrumb(" Edit")]
        public async Task<IActionResult> Edit(string userId)
        {
            if (userId == null)
            {
                return NotFound();
            }
            var applicationUser = await userManager.FindByIdAsync(userId);
            applicationUser.IsPasswordChangeRequired = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !applicationUser.IsPasswordChangeRequired : true;
            return View(applicationUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(string id)
        {
            var user = await context.ApplicationUser.FirstOrDefaultAsync(a => a.Id.ToString() == id);
            if (user is not null)
            {
                user.Updated = DateTime.Now;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.ProfilePictureUrl = null;
                await context.SaveChangesAsync();
                notifyService.Success($"Image deleted successfully.", 3);
                return Ok(new { message = "succes", succeeded = true });
            }
            notifyService.Error("image not found!");
            return NotFound("failed");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] ApplicationUser applicationUser)
        {
            if (id != applicationUser.Id.ToString())
            {
                notifyService.Error("user not found!");
                return NotFound();
            }

            if (applicationUser is not null)
            {
                try
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0)
                    {
                        string newFileName = user.Email + Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(Path.GetFileName(applicationUser.ProfileImage.FileName));
                        newFileName += fileExtension;
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "img", newFileName);
                        applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                        applicationUser.ProfilePictureUrl = "/img/" + newFileName;

                        using var dataStream = new MemoryStream();
                        applicationUser.ProfileImage.CopyTo(dataStream);
                        applicationUser.ProfilePicture = dataStream.ToArray();
                        applicationUser.ProfilePictureExtension = fileExtension;
                    }

                    if (user != null)
                    {
                        user.ProfileImage = applicationUser?.ProfileImage ?? user.ProfileImage;
                        user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                        user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                        user.ProfilePicture = applicationUser?.ProfilePicture ?? user.ProfilePicture;
                        user.FirstName = applicationUser?.FirstName;
                        user.LastName = applicationUser?.LastName;
                        if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                        {
                            user.Password = applicationUser.Password;
                        }
                        user.Country = applicationUser.Country;
                        user.Active = applicationUser.Active;
                        user.Addressline = applicationUser.Addressline;

                        user.CountryId = applicationUser.SelectedCountryId;
                        user.StateId = applicationUser.SelectedStateId;
                        user.DistrictId = applicationUser.SelectedDistrictId;
                        user.PinCodeId = applicationUser.SelectedPincodeId;

                        user.IsUpdated = true;
                        user.Updated = DateTime.Now;
                        user.PhoneNumber = applicationUser.PhoneNumber;
                        user.UpdatedBy = HttpContext.User?.Identity?.Name;
                        user.SecurityStamp = DateTime.Now.ToString();
                        var result = await userManager.UpdateAsync(user);
                        if (result.Succeeded)
                        {
                            var roles = await userManager.GetRolesAsync(user);
                            var roleResult = await userManager.RemoveFromRolesAsync(user, roles);
                            await userManager.AddToRoleAsync(user, user.Role.ToString());
                            var country = context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                            await smsService.DoSendSmsAsync(country.Code, country.ISDCode + user.PhoneNumber, "User edited. \n\nEmail : " + user.Email);
                            notifyService.Custom($"User edited successfully.", 3, "orange", "fas fa-user-check");
                            return RedirectToAction(nameof(Index));
                        }
                        notifyService.Error("Error !!. The user can't be edited!");
                        Errors(result);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    notifyService.Error("Error !!. The user con't be edited!");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
            }

            notifyService.Error("Error !!. The user con't be edited!");
            return RedirectToAction(nameof(User));
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}