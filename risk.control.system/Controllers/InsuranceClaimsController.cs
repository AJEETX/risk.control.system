using Microsoft.AspNetCore.Mvc;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;

using SmartBreadcrumbs.Attributes;
using risk.control.system.Data;
using AspNetCoreHero.ToastNotification.Notyf;
using risk.control.system.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    public class InsuranceClaimsController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IClaimPolicyService claimPolicyService;
        private readonly IClaimsVendorService vendorService;
        private readonly INotyfService notifyService;

        public InsuranceClaimsController(ApplicationDbContext context,
            IClaimPolicyService claimPolicyService,
            IClaimsVendorService vendorService,
            INotyfService notifyService
            )
        {
            this.context = context;
            this.claimPolicyService = claimPolicyService;
            this.vendorService = vendorService;
            this.notifyService = notifyService;
        }

        [Breadcrumb(" Add New", FromAction = "Incomplete", FromController = typeof(ClaimsInvestigationController))]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public IActionResult Index()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            var claim = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId
                }
            };
            var companyUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = context.ClaimsInvestigation.Include(c => c.PolicyDetail).Where(c => !c.Deleted && c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated.Count;

                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                    notifyService.Information($"MAX Claim limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                }
                else
                {
                    notifyService.Information($"Limit available = <b>{availableCount}</b>");
                }
            }
            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = null,
                AllowedToCreate = userCanCreate,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                Location = new CaseLocation { },
                AvailableCount = availableCount,
                TotalCount = companyUser.ClientCompany.TotalCreatedClaimAllowed,
                Trial = trial
            };

            return View(model);
        }
        public async Task<IActionResult> Summary4Insurer(string id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index));
                }

                var model = await claimPolicyService.GetClaimSummary(currentUserEmail, id);

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin !!!..");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        public async Task<IActionResult> Summary4Agency(string id)
        {
            try
            {
                if (id == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await vendorService.GetClaimsDetails(currentUserEmail, id);
                if(model == null)
                {
                    notifyService.Error("OOPs !!!..Claim no longer exist");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}