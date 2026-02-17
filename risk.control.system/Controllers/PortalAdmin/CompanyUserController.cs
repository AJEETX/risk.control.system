using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.PortalAdmin
{
    [Breadcrumb(" Users", FromAction = "Details", FromController = typeof(ClientCompanyController))]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class CompanyUserController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IFileStorageService fileStorageService;
        private readonly IPasswordHasher<ApplicationUser> passwordHasher;
        private readonly INotyfService notifyService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<CompanyUserController> logger;
        private string portal_base_url = string.Empty;

        public CompanyUserController(UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorageService,
            IPasswordHasher<ApplicationUser> passwordHasher,
            INotyfService notifyService,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService,
             IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            ILogger<CompanyUserController> logger,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.fileStorageService = fileStorageService;
            this.passwordHasher = passwordHasher;
            this.notifyService = notifyService;
            this.roleManager = roleManager;
            smsService = SmsService;
            this.httpContextAccessor = httpContextAccessor;
            this.featureManager = featureManager;
            this.logger = logger;
            this._context = context;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            portal_base_url = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        public async Task<IActionResult> Index(long id)
        {
            var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == id);

            var agencysPage = new MvcBreadcrumbNode("Companies", "ClientCompany", "Admin Settings");
            var agency2Page = new MvcBreadcrumbNode("Companies", "ClientCompany", "Companies") { Parent = agencysPage, };
            var agencyPage = new MvcBreadcrumbNode("Details", "ClientCompany", "Company Profile") { Parent = agency2Page, RouteValues = new { id = id } };
            var editPage = new MvcBreadcrumbNode("Index", "CompanyUser", $"Users") { Parent = agencyPage };
            ViewData["BreadcrumbNode"] = editPage;

            return View(company);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (id <= 0)
            {
                return Problem("Entity set 'ApplicationDbContext.ApplicationUser'  is null.");
            }
            var clientCompanyApplicationUser = await _context.ApplicationUser.FindAsync(id);
            if (clientCompanyApplicationUser != null)
            {
                clientCompanyApplicationUser.Updated = DateTime.UtcNow;
                clientCompanyApplicationUser.UpdatedBy = HttpContext.User?.Identity?.Name;
                _context.ApplicationUser.Remove(clientCompanyApplicationUser);
            }

            await _context.SaveChangesAsync();
            notifyService.Error($"User deleted successfully.", 3);
            return RedirectToAction(nameof(CompanyUserController.Index), "CompanyUser", new { id = clientCompanyApplicationUser.ClientCompanyId });
        }
    }
}