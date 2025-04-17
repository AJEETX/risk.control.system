using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;


using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Helpers;
using SmartBreadcrumbs.Nodes;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Cases")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class InvestigationController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;

        public InvestigationController(ApplicationDbContext context, INotyfService notifyService, IInvestigationService service)
        {
            this.context = context;
            this.notifyService = notifyService;
            this.service = service;
        }
        public async Task<IActionResult> New()
        {
            try
            {
                bool userCanCreate = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = context.Investigations.Count(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
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
                    AutoAllocation = companyUser.ClientCompany.AutoAllocation
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(New));
            }
        }
        public IActionResult Create()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var model = service.Create(currentUserEmail);
                if (model.Trial)
                {
                    if (!model.AllowedToCreate)
                    {
                        notifyService.Information($"MAX Case limit = <b>{model.TotalCount}</b> reached");
                    }
                    else
                    {
                        notifyService.Information($"Limit available = <b>{model.AvailableCount}</b>");
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(Create));
            }
        }
        public async Task<IActionResult> CreatePolicy()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                ViewData["CaseEnablerId"] = new SelectList(context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");

                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = service.AddCasePolicy(currentUserEmail);
                    return View(model);
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }
        public async Task<IActionResult> EditPolicy(long id)
        {
            if (id < 0)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var claimsInvestigation = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Case Not Found!!!");
                    return RedirectToAction(nameof(CreatePolicy));
                }
                var currentUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                ViewData["lineOfBusinessId"] = new SelectList(context.LineOfBusiness.OrderBy(s => s.Code), "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

                ViewData["InvestigationServiceTypeId"] = new SelectList(context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);

                var claimsPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Cases");
                var agencyPage = new MvcBreadcrumbNode("New", "CreatorAuto", "Assign") { Parent = claimsPage, };
                var details1Page = new MvcBreadcrumbNode("Details", "CreatorAuto", $"Details") { Parent = agencyPage, RouteValues = new { id = id } };
                var editPage = new MvcBreadcrumbNode("EditPolicy", "CreatorAuto", $"Edit Case") { Parent = details1Page, RouteValues = new { id = id } };
                ViewData["BreadcrumbNode"] = editPage;

                return View(claimsInvestigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(CreatePolicy));
            }
        }

        public async Task<IActionResult> Details(long id)
        {
            if (id < 1)
            {
                notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var currentUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                var model = await service.GetClaimDetails(currentUserEmail, id);
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
