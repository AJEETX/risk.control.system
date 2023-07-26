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
    [Breadcrumb(" Agency")]
    public class VendorUserController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IPasswordHasher<VendorApplicationUser> passwordHasher;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext context;

        public VendorUserController(UserManager<VendorApplicationUser> userManager,
            IPasswordHasher<VendorApplicationUser> passwordHasher,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IToastNotification toastNotification,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.passwordHasher = passwordHasher;
            this.roleManager = roleManager;
            this.webHostEnvironment = webHostEnvironment;
            this.toastNotification = toastNotification;
            this.context = context;
            UserList = new List<UsersViewModel>();
        }

        public async Task<IActionResult> Index(string id, string sortOrder, string currentFilter, string searchString, int? currentPage, int pageSize = 10)
        {
            ViewBag.EmailSortParm = string.IsNullOrEmpty(sortOrder) ? "email_desc" : "";
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.PincodeSortParm = string.IsNullOrEmpty(sortOrder) ? "pincode_desc" : "";
            if (searchString != null)
            {
                currentPage = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;

            var vendor = await context.Vendor.Include(v => v.VendorApplicationUser).FirstOrDefaultAsync(v => v.VendorId == id);

            var applicationDbContext = vendor.VendorApplicationUser.AsQueryable();
            var model = new VendorUsersViewModel
            {
                Vendor = vendor,
            };
            //if (applicationDbContext.Any())
            {
                if (!string.IsNullOrEmpty(searchString))
                {
                    applicationDbContext = applicationDbContext.Where(a =>
                    a.FirstName.ToLower().Contains(searchString.Trim().ToLower()) ||
                    a.LastName.ToLower().Contains(searchString.Trim().ToLower()));
                }

                switch (sortOrder)
                {
                    case "name_desc":
                        applicationDbContext = applicationDbContext.OrderByDescending(s => new { s.FirstName, s.LastName });
                        break;

                    case "email_desc":
                        applicationDbContext = applicationDbContext.OrderByDescending(s => s.Email);
                        break;

                    case "pincode_desc":
                        applicationDbContext = applicationDbContext.OrderByDescending(s => s.PinCode.Code);
                        break;

                    default:
                        applicationDbContext.OrderByDescending(s => s.Email);
                        break;
                }
                int pageNumber = (currentPage ?? 1);
                ViewBag.TotalPages = (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.ShowPrevious = pageNumber > 1;
                ViewBag.ShowNext = pageNumber < (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));
                ViewBag.ShowFirst = pageNumber != 1;
                ViewBag.ShowLast = pageNumber != (int)Math.Ceiling(decimal.Divide(applicationDbContext.Count(), pageSize));

                applicationDbContext = applicationDbContext.Skip((pageNumber - 1) * pageSize).Take(pageSize);

                foreach (var user in applicationDbContext)
                {
                    var country = context.Country.FirstOrDefault(c => c.CountryId == user.CountryId);
                    var state = context.State.FirstOrDefault(c => c.StateId == user.StateId);
                    var district = context.District.FirstOrDefault(c => c.DistrictId == user.DistrictId);
                    var pinCode = context.PinCode.FirstOrDefault(c => c.PinCodeId == user.PinCodeId);

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
                    thisViewModel.VendorId = vendor.VendorId;
                    thisViewModel.ProfileImageInByte = user.ProfilePicture;
                    thisViewModel.Roles = await GetUserRoles(user);
                    UserList.Add(thisViewModel);
                }

                model.Users = UserList;
            }

            return View(model);
        }

        private async Task<List<string>> GetUserRoles(VendorApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}