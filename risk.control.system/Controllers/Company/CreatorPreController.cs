using AspNetCoreHero.ToastNotification.Notyf;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;

using SmartBreadcrumbs.Attributes;
using AspNetCoreHero.ToastNotification.Abstractions;
using risk.control.system.Data;
using risk.control.system.Services;
using static risk.control.system.AppConstant.Applicationsettings;
using Microsoft.EntityFrameworkCore;
using Google.Api;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Nodes;
using risk.control.system.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace risk.control.system.Controllers.Company
{
    [Breadcrumb(" Underwriting")]
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public partial class CreatorPreController : Controller
    {
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly ICreatorService creatorService;
        private readonly IManageCaseService manageCaseService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IMailboxService mailboxService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public CreatorPreController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IClaimsInvestigationService claimsInvestigationService,
            ICreatorService creatorService,
            IManageCaseService manageCaseService,
            IFtpService ftpService,
            INotyfService notifyService,
            IMailboxService mailboxService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.creatorService = creatorService;
            this.manageCaseService = manageCaseService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.mailboxService = mailboxService;
            this.investigationReportService = investigationReportService;
        }
        public IActionResult Index()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                return RedirectToAction("New");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Assign")]
        public IActionResult New(CREATEDBY? mode)
        {
            try
            {
                bool userCanCreate = true;
                int availableCount = 0;
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(u => u.Email == currentUserEmail);
                if (companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial)
                {
                    var totalClaimsCreated = _context.CaseVerification.Count(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
                    availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated;
                    if (totalClaimsCreated >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                    {
                        userCanCreate = false;
                        notifyService.Information($"MAX Case limit = <b>{companyUser.ClientCompany.TotalCreatedClaimAllowed}</b> reached");
                    }
                    else
                    {
                        notifyService.Information($"Limit available = <b>{availableCount}</b>");
                    }
                }
                var createdClaimsStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(s => s.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                var hasClaim = _context.CaseVerification.Any(c => c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId &&
                !c.Deleted &&
                c.InvestigationCaseSubStatus == createdClaimsStatus);
                var fileIdentifier = companyUser.ClientCompany.Country.Code.ToLower();
                if (mode is not null)
                {
                    ViewBag.ActiveTab = mode;
                }
                else
                {
                    ViewBag.ActiveTab = CREATEDBY.AUTO;
                }
                return View(new CreateClaims { BulkUpload = companyUser.ClientCompany.BulkUpload, UserCanCreate = userCanCreate, HasClaims = hasClaim, FileSampleIdentifier = fileIdentifier });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(New));
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(IFormFile postedFile, CreateClaims model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);

                if (postedFile == null || model == null ||
                string.IsNullOrWhiteSpace(Path.GetFileName(postedFile.FileName)) ||
                string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(postedFile.FileName))) ||
                Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip"
                )
                {
                    notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");
                    if (model == null)
                    {
                        return RedirectToAction(nameof(Index), "Dashboard");
                    }
                    return RedirectToAction(nameof(CreatorPreController.New), "CreatorPre");

                }

                bool processed = false;
                //if (model.Uploadtype == UploadType.FTP)
                //{
                //    processed = await ftpService.UploadFtpFile(currentUserEmail, postedFile, model.CREATEDBY);
                //}

                if (model.Uploadtype == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                {

                    processed = await ftpService.UploadCaseFile(currentUserEmail, postedFile, model.CREATEDBY);
                }

                if (processed)
                {
                    notifyService.Custom($"{model.Uploadtype.GetEnumDisplayName()} complete ", 3, "green", "fa fa-upload");
                }
                else
                {
                    notifyService.Information($"{model.Uploadtype.GetEnumDisplayName()} Error. Check limit <i class='fa fa-upload' ></i>", 3);
                }
                return RedirectToAction(nameof(CreatorPreController.New), "CreatorPre");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == currentUserEmail);

                var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors)
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                //IF AUTO ALLOCATION TRUE
                if (company.AutoAllocation)
                {
                    var autoAllocatedClaims = await claimsInvestigationService.ProcessAutoAllocation(claims, company, currentUserEmail);

                    if (claims.Count == autoAllocatedClaims.Count)
                    {
                        notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                    }

                    else if (claims.Count > autoAllocatedClaims.Count)
                    {
                        if (autoAllocatedClaims.Count > 0)
                        {
                            notifyService.Custom($"{autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-assigned", 3, "green", "far fa-file-powerpoint");
                        }

                        var notAutoAllocated = claims.Except(autoAllocatedClaims)?.ToList();

                        await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                        await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                        notifyService.Custom($"{notAutoAllocated.Count}/{claims.Count} case(s) need assign manually", 3, "orange", "far fa-file-powerpoint");

                        return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
                    }
                }
                else
                {
                    await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

                    await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

                    notifyService.Custom($"{claims.Count}/{claims.Count} case(s) assigned", 3, "green", "far fa-file-powerpoint");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
            }
            return RedirectToAction(nameof(ClaimsActiveController.Active), "ClaimsActive");
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "New")]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(selectedcase))
                {
                    notifyService.Error("No case selected!!!. Please select case to be allocate.");
                    return RedirectToAction(nameof(New));
                }

                var model = await empanelledAgencyService.GetEmpanelledVendors(selectedcase);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
        [Breadcrumb(" Add New", FromAction = "New")]
        public IActionResult Create()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

                ViewData["lineOfBusinessId"] = lineOfBusinessId;
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");

                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == lineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name");
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (currentUser.ClientCompany.HasSampleData)
                {
                    var model = manageCaseService.AddCase(currentUserEmail, lineOfBusinessId);
                    return View(model);
                }
                else
                {
                    var blankCustomerDetail = new Claimant { Country = currentUser.ClientCompany.Country, CountryId = currentUser.ClientCompany.CountryId };
                    var model = new CaseVerification { CustomerDetail = blankCustomerDetail };
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Try Again");
                return RedirectToAction(nameof(Create));
            }
        }
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaseVerification model, long selectedCountryId, long selectedStateId, long selectedDistrictId, long selectedPincodeId)
        {
            if (selectedCountryId < 1 || selectedStateId < 1 || selectedDistrictId < 1 || selectedPincodeId < 1)
            {
                notifyService.Error("OOPs !!!..Case Not Found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                model.CustomerDetail.CountryId = selectedCountryId;
                model.CustomerDetail.StateId = selectedStateId;
                model.CustomerDetail.DistrictId = selectedDistrictId;
                model.CustomerDetail.PinCodeId = selectedPincodeId;

                IFormFile documentFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == model.CustomerDetail?.ProfileImage?.FileName && f.Name == model.CustomerDetail?.ProfileImage?.Name);
                    if (file != null && file.Length > 2000000)
                    {
                        notifyService.Warning("Uploaded File size morer than 2MB !!! ");
                        return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto", new { model });
                    }
                    if (file != null)
                    {
                        documentFile = file;
                    }
                }
                var claim = await manageCaseService.Create(currentUserEmail, model, documentFile);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error creating case");
                }
                else
                {
                    notifyService.Custom($"Case #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(CreatorPreController.New), "CreatorPre");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(" Edit", FromAction = "New")]
        public IActionResult Edit(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id <= 0)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = manageCaseService.GetCases().Include(p=>p.PolicyDetail).ThenInclude(p => p.LineOfBusiness).FirstOrDefault(c => c.CaseVerificationId == id);
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
               i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", model.PolicyDetail.CaseEnablerId);
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CaseVerification model, long selectedCountryId, long selectedStateId, long selectedDistrictId, long selectedPincodeId)
        {
            if (selectedCountryId < 1 || selectedStateId < 1 || selectedDistrictId < 1 || selectedPincodeId < 1)
            {
                notifyService.Error("OOPs !!!..Case Not Found");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                model.CustomerDetail.CountryId = selectedCountryId;
                model.CustomerDetail.StateId = selectedStateId;
                model.CustomerDetail.DistrictId = selectedDistrictId;
                model.CustomerDetail.PinCodeId = selectedPincodeId;

                IFormFile documentFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == model.CustomerDetail?.ProfileImage?.FileName && f.Name == model.CustomerDetail?.ProfileImage?.Name);
                    if (file != null && file.Length > 2000000)
                    {
                        notifyService.Warning("Uploaded File size morer than 2MB !!! ");
                        return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto", new { model });
                    }
                    if (file != null)
                    {
                        documentFile = file;
                    }
                }
                var claim = await manageCaseService.Edit(currentUserEmail, model, documentFile);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error creating case");
                }
                else
                {
                    notifyService.Custom($"Case #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(CreatorPreController.New), "CreatorPre");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Delete", FromAction = "New")]
        public IActionResult Delete(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (id <= 0)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var model = manageCaseService.GetCases().FirstOrDefault(c=>c.CaseVerificationId == id);
                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefault(c => c.Email == currentUserEmail);
                ViewData["Currency"] = Extensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
                if (model == null)
                {
                    notifyService.Error("OOPS!!!.Case Not Found.Try Again");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }

        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(CaseVerification model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);

                if (model is null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var claimsInvestigation = await _context.CaseVerification.FindAsync(model.CaseVerificationId);
                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.CaseVerification.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                notifyService.Custom("Case deleted", 3, "red", "far fa-file-powerpoint");
                if (companyUser.ClientCompany.AutoAllocation)
                {
                    if (model.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        ViewBag.ActiveTab = CREATEDBY.MANUAL;
                    }
                    else
                    {
                        ViewBag.ActiveTab = CREATEDBY.AUTO;
                    }
                    return RedirectToAction(nameof(CreatorPreController.New), "CreatorPre", new { mode = ViewBag.ActiveTab });
                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}
