using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    [Breadcrumb(" Users")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        public List<UsersViewModel> UserList;
        private readonly ApplicationDbContext context;
        private IPasswordHasher<ApplicationUser> passwordHasher;

        public UserController(UserManager<ApplicationUser> userManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            INotyfService notifyService,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.passwordHasher = passwordHasher;
            this.webHostEnvironment = webHostEnvironment;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
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
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user)
        {
            if (user.ProfileImage != null && user.ProfileImage.Length > 0)
            {
                string newFileName = Guid.NewGuid().ToString();
                string fileExtension = Path.GetExtension(user.ProfileImage.FileName);
                newFileName += fileExtension;
                var upload = Path.Combine(webHostEnvironment.WebRootPath, "img", newFileName);
                user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                user.ProfilePictureUrl = "/img/" + newFileName;
                using var dataStream = new MemoryStream();
                user.ProfileImage.CopyTo(dataStream);
                user.ProfilePicture = dataStream.ToArray();
            }
            user.EmailConfirmed = true;
            user.Email = user.Email.Trim().ToLower();
            user.UserName = user.Email;
            user.Mailbox = new Mailbox { Name = user.Email };
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = HttpContext.User?.Identity?.Name;
            IdentityResult result = await userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                notifyService.Custom($"User created successfully.", 3, "green", "fas fa-user-plus");
                var response = SmsService.SendSingleMessage(user.PhoneNumber, "User created. Email : " + user.Email);

                return RedirectToAction(nameof(Index));
            }
            else
            {
                toastNotification.AddErrorToastMessage("Error to create user!");
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            GetCountryStateEdit(user);
            return View(user);
        }

        private void GetCountryStateEdit(ApplicationUser? applicationUser)
        {
            var country = context.Country.OrderBy(o => o.Name);
            var relatedStates = context.State.Include(s => s.Country).Where(s => s.Country.CountryId == applicationUser.CountryId).OrderBy(d => d.Name);
            var districts = context.District.Include(d => d.State).Where(d => d.State.StateId == applicationUser.StateId).OrderBy(d => d.Name);
            var pincodes = context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == applicationUser.DistrictId).OrderBy(d => d.Name);

            ViewData["CountryId"] = new SelectList(country, "CountryId", "Name", applicationUser.CountryId);
            ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", applicationUser.StateId);
            ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", applicationUser.DistrictId);
            ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", applicationUser.PinCodeId);
        }

        [Breadcrumb(" Edit")]
        public async Task<IActionResult> Edit(string userId)
        {
            if (userId == null)
            {
                return NotFound();
            }

            var applicationUser = await userManager.FindByIdAsync(userId);

            GetCountryStateEdit(applicationUser);

            if (applicationUser != null)
                return View(applicationUser);
            else
            {
                toastNotification.AddErrorToastMessage("user not found!");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(string id)
        {
            var user = await context.ApplicationUser.FirstOrDefaultAsync(a => a.Id.ToString() == id);
            if (user is not null)
            {
                user.Updated = DateTime.UtcNow;
                user.UpdatedBy = HttpContext.User?.Identity?.Name;
                user.ProfilePictureUrl = null;
                await context.SaveChangesAsync();
                notifyService.Error($"Image deleted successfully.", 3);
                return Ok(new { message = "succes", succeeded = true });
            }
            toastNotification.AddErrorToastMessage("image not found!");
            return NotFound("failed");
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] ApplicationUser applicationUser)
        {
            if (id != applicationUser.Id.ToString())
            {
                toastNotification.AddErrorToastMessage("user not found!");
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
                        string fileExtension = Path.GetExtension(applicationUser.ProfileImage.FileName);
                        newFileName += fileExtension;
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "img", newFileName);
                        applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                        applicationUser.ProfilePictureUrl = "/img/" + newFileName;

                        using var dataStream = new MemoryStream();
                        applicationUser.ProfileImage.CopyTo(dataStream);
                        applicationUser.ProfilePicture = dataStream.ToArray();
                    }

                    if (user != null)
                    {
                        user.ProfileImage = applicationUser?.ProfileImage ?? user.ProfileImage;
                        user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                        user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                        user.ProfilePicture = applicationUser?.ProfilePicture;
                        user.FirstName = applicationUser?.FirstName;
                        user.LastName = applicationUser?.LastName;
                        if (!string.IsNullOrWhiteSpace(applicationUser?.Password))
                        {
                            user.Password = applicationUser.Password;
                        }
                        user.Country = applicationUser.Country;
                        user.Active = applicationUser.Active;
                        user.Addressline = applicationUser.Addressline;
                        user.CountryId = applicationUser.CountryId;
                        user.State = applicationUser.State;
                        user.StateId = applicationUser.StateId;
                        user.PinCode = applicationUser.PinCode;
                        user.PinCodeId = applicationUser.PinCodeId;
                        user.Updated = DateTime.UtcNow;
                        user.PhoneNumber = applicationUser.PhoneNumber;
                        user.UpdatedBy = HttpContext.User?.Identity?.Name;
                        user.SecurityStamp = DateTime.UtcNow.ToString();
                        var result = await userManager.UpdateAsync(user);
                        if (result.Succeeded)
                        {
                            var response = SmsService.SendSingleMessage(user.PhoneNumber, "User edited. Email : " + user.Email);
                            notifyService.Custom($"User edited successfully.", 3, "orange", "fas fa-user-check");
                            return RedirectToAction(nameof(Index));
                        }
                        toastNotification.AddErrorToastMessage("Error !!. The user can't be edited!");
                        Errors(result);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                }
            }

            toastNotification.AddErrorToastMessage("Error !!. The user con't be edited!");
            return RedirectToAction(nameof(User));
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}