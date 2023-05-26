using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class CompanyUserController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IPasswordHasher<ClientCompanyApplicationUser> passwordHasher;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IToastNotification toastNotification;
        private readonly ApplicationDbContext _context;

        public CompanyUserController(UserManager<ClientCompanyApplicationUser> userManager,
            IPasswordHasher<ClientCompanyApplicationUser> passwordHasher,
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
            this._context = context;
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

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .FirstOrDefault(c => c.ClientCompanyId == id);

            var applicationDbContext = company.CompanyApplicationUser
                .AsQueryable();

            applicationDbContext = applicationDbContext
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.District)
                .Include(c => c.PinCode)
                .Include(c=>c.ClientCompany)
                .Where(u => u.ClientCompanyId == id);

            var model = new CompanyUsersViewModel
            {
                Company = company,
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

                var users = applicationDbContext.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                var tempusers = userManager.Users.Where(c=>c.ClientCompanyId == id);
                foreach (var user in users)
                {
                    var country = _context.Country.FirstOrDefault(c=>c.CountryId == user.CountryId);
                    var state = _context.State.FirstOrDefault(c=>c.StateId == user.StateId);
                    var district = _context.District.FirstOrDefault(c=>c.DistrictId == user.DistrictId);
                    var pinCode = _context.PinCode.FirstOrDefault(c=>c.PinCodeId == user.PinCodeId);

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
            }
            return View(model);
        }
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.ClientCompanyApplicationUser == null)
            {
                return NotFound();
            }

            var clientApplicationUser = await _context.ClientCompanyApplicationUser
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.ClientCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clientApplicationUser == null)
            {
                return NotFound();
            }

            return View(clientApplicationUser);
        }

        // GET: ClientCompanyApplicationUser/Create
        public IActionResult Create(string id)
        {
            var company = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == id);
            var model = new ClientCompanyApplicationUser { ClientCompany = company };
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            return View(model);
        }

        // POST: ClientCompanyApplicationUser/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCompanyApplicationUser clientCompanyApplicationUser)
        {

            if (clientCompanyApplicationUser is not null)
            {
                clientCompanyApplicationUser.Mailbox.Name = clientCompanyApplicationUser.Email;
                IFormFile? vendorUserProfile = Request.Form?.Files?.FirstOrDefault();
                if (vendorUserProfile is not null)
                {
                    clientCompanyApplicationUser.ProfileImage = vendorUserProfile;
                    using var dataStream = new MemoryStream();
                    await clientCompanyApplicationUser.ProfileImage.CopyToAsync(dataStream);
                    clientCompanyApplicationUser.ProfilePicture = dataStream.ToArray();
                }

                _context.Add(clientCompanyApplicationUser);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("company user created successfully!");
                return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompanyId });
            }
            toastNotification.AddErrorToastMessage("Error to create company user!");
            return Problem();
        }

        // GET: ClientCompanyApplicationUser/Edit/5
        public async Task<IActionResult> Edit(long? userId)
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
        public async Task<IActionResult> Edit(long id, ClientCompanyApplicationUser clientCompanyApplicationUser)
        {
            if (id != clientCompanyApplicationUser.Id)
            {
                toastNotification.AddErrorToastMessage("company not found!");
                return NotFound();
            }

            if (clientCompanyApplicationUser is not null)
            {
                try
                {
                    clientCompanyApplicationUser.Mailbox.Name = clientCompanyApplicationUser.Email;
                    IFormFile? vendorUserProfile = Request.Form?.Files?.FirstOrDefault();
                    if (vendorUserProfile is not null)
                    {
                        clientCompanyApplicationUser.ProfileImage = vendorUserProfile;
                        using var dataStream = new MemoryStream();
                        await clientCompanyApplicationUser.ProfileImage.CopyToAsync(dataStream);
                        clientCompanyApplicationUser.ProfilePicture = dataStream.ToArray();
                    }

                    _context.ClientCompanyApplicationUser.Update(clientCompanyApplicationUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorApplicationUserExists(clientCompanyApplicationUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                toastNotification.AddSuccessToastMessage("vendor user edited successfully!");
                return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompanyId });
            }

            toastNotification.AddErrorToastMessage("Error to create vendor user!");
            return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompany });
        }

        // GET: VendorApplicationUsers/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null || _context.VendorApplicationUser == null)
            {
                return NotFound();
            }

            var vendorApplicationUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorApplicationUser == null)
            {
                return NotFound();
            }

            return View(vendorApplicationUser);
        }

        // POST: VendorApplicationUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.ClientCompanyApplicationUser == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VendorApplicationUser'  is null.");
            }
            var clientCompanyApplicationUser = await _context.ClientCompanyApplicationUser.FindAsync(id);
            if (clientCompanyApplicationUser != null)
            {
                _context.ClientCompanyApplicationUser.Remove(clientCompanyApplicationUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompanyId });
        }

        private bool VendorApplicationUserExists(long id)
        {
            return (_context.VendorApplicationUser?.Any(e => e.Id == id)).GetValueOrDefault();
        }
        private async Task<List<string>> GetUserRoles(ClientCompanyApplicationUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}
