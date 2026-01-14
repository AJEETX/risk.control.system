using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    [Breadcrumb(" Cases")]
    public class CaseUploadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvestigationService service;
        private readonly ILogger<CaseUploadController> logger;
        private readonly INotyfService notifyService;

        public CaseUploadController(ApplicationDbContext context,
            IInvestigationService service,
            ILogger<CaseUploadController> logger,
            INotyfService notifyService)
        {
            _context = context;
            this.service = service;
            this.logger = logger;
            this.notifyService = notifyService;
        }

        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                if (userRole.Value.Contains(CREATOR.DISPLAY_NAME))
                {
                    return RedirectToAction("Uploads");
                }
                else if (userRole.Value.Contains(ASSESSOR.DISPLAY_NAME))
                {
                    return RedirectToAction("Assessor");
                }
                else if (userRole.Value.Contains(MANAGER.DISPLAY_NAME))
                {
                    return RedirectToAction("Manager");
                }
                else
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Upload File")]
        public async Task<IActionResult> Uploads(int uploadId = 0)
        {
            try
            {
                bool userCanCreate = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial)
                {
                    var totalClaimsCreated = await _context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                    availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated;
                    if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanCreate = false;
                        notifyService.Information($"MAX Case limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                    }
                }
                var totalReadyToAssign = await service.GetAutoCount(currentUserEmail);
                var hasClaim = totalReadyToAssign > 0;
                var fileIdentifier = companyUser.ClientCompany.Country.Code.ToLower();
                var hasFileUploads = _context.FilesOnFileSystem.Any();
                var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
                userCanCreate = userCanCreate && companyUser.ClientCompany.TotalToAssignMaxAllowed > totalReadyToAssign;

                if (!userCanCreate)
                {
                    notifyService.Custom($"MAX Assign Case limit = <b>{companyUser.ClientCompany.TotalToAssignMaxAllowed}</b> reached", 5, "#dc3545", "fa fa-upload");
                }
                return View(new CreateClaims
                {
                    BulkUpload = companyUser.ClientCompany.BulkUpload,
                    UserCanCreate = userCanCreate,
                    HasClaims = hasClaim,
                    FileSampleIdentifier = fileIdentifier,
                    UploadId = uploadId,
                    HasFileUploads = hasFileUploads,
                    IsManager = isManager,
                    AutoAllocation = companyUser.ClientCompany.AutoAllocation
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Uploads), "ClaimsLog");
            }
        }
    }
}