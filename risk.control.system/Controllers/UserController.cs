using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        public List<UsersViewModel> UserList;
        private readonly ApplicationDbContext context;
        private IPasswordHasher<ApplicationUser> passwordHasher;

        public UserController(UserManager<ApplicationUser> userManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.passwordHasher = passwordHasher;
            this.webHostEnvironment = webHostEnvironment;
            this.context = context;
            UserList = new List<UsersViewModel>();
        }
        public async Task<IActionResult> Index()
        {
            var users = await userManager.Users.Include(u => u.Country).Include(u => u.State).Include(u => u.PinCode).ToListAsync();
            foreach (Models.ApplicationUser user in users)
            {
                var thisViewModel = new UsersViewModel();
                thisViewModel.UserId = user.Id.ToString();
                thisViewModel.Email = user?.Email;
                thisViewModel.UserName = user?.UserName;
                thisViewModel.ProfileImage = user?.ProfilePictureUrl ?? "img/no-image.png";
                thisViewModel.FirstName = user.FirstName;
                thisViewModel.LastName = user.LastName;
                thisViewModel.Country = user.Country.Name;
                thisViewModel.CountryId = user.CountryId;
                thisViewModel.StateId = user.StateId;
                thisViewModel.State = user.State.Name;
                thisViewModel.PinCode = user.PinCode.Name;
                thisViewModel.PinCodeId = user.PinCode.PinCodeId;
                thisViewModel.Roles = await GetUserRoles(user);
                UserList.Add(thisViewModel);
            }
            return View(UserList);
        }

        public IActionResult Create()
        {
            ViewData["CountryId"] = new SelectList(context.Country, "CountryId", "Name");
            return View();
        }
        [HttpPost, ActionName("GetStatesByCountryId")]
        public async Task<JsonResult> GetStatesByCountryId(string countryId) {
            string cId;
            var states = new List <State> ();
            if (!string.IsNullOrEmpty(countryId)) {
                cId = countryId;
                states = await context.State.Where(s => s.CountryId.Equals(cId)).ToListAsync();
            }
            return Json(states);
        }

        [HttpPost, ActionName("GetDistrictByStateId")]
        public async Task<JsonResult> GetDistrictByStateId(string stateId) {
            string sId;
            var districts = new List <District> ();
            if (!string.IsNullOrEmpty(stateId)) {
                sId = stateId;
                districts = await context.District.Where(s => s.State.StateId.Equals(sId)).ToListAsync();
            }
            return Json(districts);
        }

        [HttpPost, ActionName("GetPinCodesByDistrictId")]
        public async Task<JsonResult> GetPinCodesByDistrictId(string districtId)
        {
            string sId;
            var pincodes = new List<PinCode>();
            if (!string.IsNullOrEmpty(districtId))
            {
                sId = districtId;
                pincodes = await context.PinCode.Where(s => s.District.DistrictId.Equals(sId)).ToListAsync();
            }
            return Json(pincodes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user)
        {
            user.Id = Guid.NewGuid();
            {
                if(user.ProfileImage != null && user.ProfileImage.Length >0 )
                {
                    string newFileName = Guid.NewGuid().ToString();
                    string fileExtension = Path.GetExtension(user.ProfileImage.FileName);
                    newFileName += fileExtension;
                    var upload = Path.Combine(webHostEnvironment.WebRootPath, "upload", newFileName);
                    user.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                    user.ProfilePictureUrl = "upload/"+newFileName;
                }

                IdentityResult result = await userManager.CreateAsync(user, user.Password);

                if (result.Succeeded)
                    return RedirectToAction(nameof(Index));
                else
                {
                    foreach (IdentityError error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
            }
            GetCountryStateEdit(user);
            return View(user);
        }
        private void GetCountryStateEdit(ApplicationUser? user)
        {
            ViewData["CountryId"] = new SelectList(context.Country, "CountryId", "Name",user?.CountryId);
            ViewData["StateId"] = new SelectList(context.State.Where(s => s.CountryId == user.CountryId ), "StateId", "Name", user?.StateId);
            ViewData["PinCodeId"] = new SelectList(context.PinCode.Where(s => s.StateId == user.StateId ), "PinCodeId", "Name", user?.PinCodeId);
        }
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
                return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteImage(string id)
        {
            var user = await context.ApplicationUser.FirstOrDefaultAsync(a => a.Id.ToString() == id);
            if(user is not null)
            {
                user.ProfilePictureUrl = null;
                await context.SaveChangesAsync();
                return Ok(new { message = "succes", succeeded = true });
            }
            return NotFound("failed");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] ApplicationUser applicationUser)
        {
            if (id != applicationUser.Id.ToString())
            {
                return NotFound();
            }

            {
                try
                {
                    var user = await userManager.FindByIdAsync(id);
                    if(applicationUser?.ProfileImage != null && applicationUser.ProfileImage.Length > 0 )
                    {
                        string newFileName = Guid.NewGuid().ToString();
                        string fileExtension = Path.GetExtension(applicationUser.ProfileImage.FileName);
                        newFileName += fileExtension;
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "upload", newFileName);
                        applicationUser.ProfileImage.CopyTo(new FileStream(upload, FileMode.Create));
                        applicationUser.ProfilePictureUrl = "upload/"+ newFileName;
                    }

                    if (user != null)
                    {
                        user.ProfileImage = applicationUser?.ProfileImage ?? user.ProfileImage;
                        user.ProfilePictureUrl = applicationUser?.ProfilePictureUrl ?? user.ProfilePictureUrl;
                        user.PhoneNumber = applicationUser?.PhoneNumber ?? user.PhoneNumber;
                        user.FirstName = applicationUser?.FirstName;
                        user.LastName = applicationUser?.LastName;
                        if(!string.IsNullOrWhiteSpace(applicationUser?.Password))
                        {
                            user.Password = applicationUser.Password;
                        }
                        user.Email = applicationUser.Email;
                        user.UserName = applicationUser.UserName;
                        user.Country = applicationUser.Country;
                        user.CountryId = applicationUser.CountryId;
                        user.State = applicationUser.State;
                        user.StateId = applicationUser.StateId;
                        user.PinCode = applicationUser.PinCode;
                        user.PinCodeId = applicationUser.PinCodeId;                        
                        var result = await userManager.UpdateAsync(user);
                        if (result.Succeeded)
                        {
                            return RedirectToAction("Index");
                        }
                        Errors(result);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return Problem();
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        private async Task<List<string>> GetUserRoles(Models.ApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}
