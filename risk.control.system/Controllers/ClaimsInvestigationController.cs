using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

using System.Data;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        private static string NO_DATA = " NO - DATA ";
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;

        public ClaimsInvestigationController(ApplicationDbContext context,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            UserManager<ClientCompanyApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification)
        {
            _context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
            this.roleManager = roleManager;
            this.toastNotification = toastNotification;
        }

        [Breadcrumb(" Add New")]
        public async Task<IActionResult> CreateClaim()
        {
            var claim = new ClaimsInvestigation { PolicyDetail = new PolicyDetail { LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId } };

            var model = new ClaimTransactionModel
            {
                Claim = claim,
                Log = null,
                Location = new CaseLocation { }
            };

            return View(model);
        }

        [Breadcrumb(" Claims")]
        public IActionResult Index()
        {
            var claimsPage = new MvcBreadcrumbNode("Active", "Claims", "Claims");
            ViewData["BreadcrumbNode"] = claimsPage;
            return View();
        }

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Assign()
        {
            return View();
        }

        [Breadcrumb(" Assess", FromAction = "Index")]
        public async Task<IActionResult> Assessor()
        {
            return View();
        }

        [Breadcrumb(" Allocate", FromAction = "Index")]
        public IActionResult Assigner()
        {
            return View();
        }

        // GET: ClaimsInvestigation

        [Breadcrumb(" Draft", FromAction = "Index")]
        public IActionResult Draft()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction ="Assigner")]
        public async Task<IActionResult> EmpanelledVendors(string selectedcase)
        {
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Assigner));
            }

            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLocations = claimsInvestigation.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
            && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId).ToList();

            claimsInvestigation.CaseLocations = caseLocations;

            var location = claimsInvestigation.CaseLocations.FirstOrDefault()?.CaseLocationId;

            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.CaseLocationId == location
                //&& c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId
                );

            var existingVendors = await _context.Vendor
                .Where(c => c.ClientCompanyId == claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .ToListAsync();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claimsCases = _context.ClaimsInvestigation
                .Include(c => c.Vendors)
                .Include(c => c.CaseLocations.Where(c =>
                !string.IsNullOrWhiteSpace(c.VendorId) &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                ));

            var vendorCaseCount = new Dictionary<string, int>();

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.CaseLocations.Count > 0)
                {
                    foreach (var CaseLocation in claimsCase.CaseLocations)
                    {
                        if (!string.IsNullOrEmpty(CaseLocation.VendorId))
                        {
                            if (CaseLocation.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                    )
                            {
                                if (!vendorCaseCount.TryGetValue(CaseLocation.VendorId, out countOfCases))
                                {
                                    vendorCaseCount.Add(CaseLocation.VendorId, 1);
                                }
                                else
                                {
                                    int currentCount = vendorCaseCount[CaseLocation.VendorId];
                                    ++currentCount;
                                    vendorCaseCount[CaseLocation.VendorId] = currentCount;
                                }
                            }
                        }
                    }
                }
            }

            List<VendorCaseModel> vendorWithCaseCounts = new();

            foreach (var existingVendor in existingVendors)
            {
                var vendorCase = vendorCaseCount.FirstOrDefault(v => v.Key == existingVendor.VendorId);
                if (vendorCase.Key == existingVendor.VendorId)
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = vendorCase.Value,
                        Vendor = existingVendor,
                    });
                }
                else
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = 0,
                        Vendor = existingVendor,
                    });
                }
            }

            ViewBag.CompanyId = claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId;

            ViewBag.Selectedcase = selectedcase;
            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, Vendors = vendorWithCaseCounts, ClaimsInvestigation = claimsInvestigation });
        }

        [HttpGet]
        [Breadcrumb(" Allocate (to agency)")]
        public async Task<IActionResult> AllocateToVendor(string selectedcase)
        {
            if (string.IsNullOrWhiteSpace(selectedcase))
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be allocate.");
                return RedirectToAction(nameof(Assigner));
            }

            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var caseLocations = claimsInvestigation.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
            && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId).ToList();

            claimsInvestigation.CaseLocations = caseLocations;
            return View(claimsInvestigation);
        }

        [HttpGet]
        [Breadcrumb(" Re-allocate to agency")]
        public async Task<IActionResult> ReAllocateToVendor(string selectedcase)
        {
            if (_context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var caseLocations = claimsInvestigation.CaseLocations.Where(c => !string.IsNullOrWhiteSpace(c.VendorId)
            && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId).ToList();

            claimsInvestigation.CaseLocations = caseLocations;
            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> CaseAllocatedToVendor(string selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            await claimsInvestigationService.AllocateToVendor(userEmail, claimId, selectedcase, caseLocationId);

            await mailboxService.NotifyClaimAllocationToVendor(userEmail, claimId, selectedcase, caseLocationId);

            toastNotification.AddSuccessToastMessage("Claim allocated to agency successfully!");

            return RedirectToAction(nameof(ClaimsInvestigationController.Assigner), "ClaimsInvestigation");
        }

        [Breadcrumb(" Case-locations")]
        public IActionResult CaseLocation(string id)
        {
            if (id == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            var applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefault(a => a.ClaimsInvestigationId == id);

            return View(applicationDbContext);
        }

        [Breadcrumb(" Closed")]
        public async Task<IActionResult> Approved()
        {
            return View();
        }

        [Breadcrumb(" Rejected", FromAction = "Index")]
        public async Task<IActionResult> Reject()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State);

            ViewBag.HasClientCompany = true;
            ViewBag.HasVendorCompany = true;

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var assessorApprovedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                ViewBag.HasClientCompany = false;
                ViewBag.HasVendorCompany = false;
                applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
                ViewBag.HasVendorCompany = false;
            }

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

                var claimsSubmitted = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == assessorApprovedStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
                return View(claimsSubmitted);
            }
            return Problem();
        }

        [Breadcrumb(" Re Allocate", FromAction = "Index")]
        public async Task<IActionResult> Review()
        {
            return View();
        }

        [Breadcrumb(title: "Active")]
        public IActionResult Active()
        {
            return View();
        }

        [Breadcrumb(title: "Withdraw", FromAction = "Index")]
        public async Task<IActionResult> ToInvestigate()
        {
            return View();
        }

        [Breadcrumb(title: "Report", FromAction = "Index")]
        public async Task<IActionResult> GetInvestigateReport(string selectedcase)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId
                    );
            return View(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, ClaimsInvestigation = claimsInvestigation });
        }

        [Breadcrumb(title: "Report", FromAction = "Approved")]
        public async Task<IActionResult> GetApprovedReport(string selectedcase)
        {
            if (selectedcase == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == selectedcase)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.InvestigationCaseSubStatus)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.BeneficiaryRelation)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.Country)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.District)
              .Include(c => c.InvestigationCaseStatus)
              .Include(c => c.InvestigationCaseSubStatus)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.InvestigationServiceType)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.LineOfBusiness)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var location = await _context.CaseLocation
                .Include(l => l.ClaimReport)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == selectedcase);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            var reportUpdateStatus = AssessorRemarkType.OK;

            await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage("claim case processed successfully!");

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor));
        }

        [HttpPost]
        public async Task<IActionResult> ReProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            var reportUpdateStatus = AssessorRemarkType.REVIEW;

            await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage("claim case re-assigned successfully!");

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor));
        }

        [HttpPost]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be assigned.");
                return RedirectToAction(nameof(Assign));
            }
            await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

            await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

            toastNotification.AddSuccessToastMessage("case(s) assigned successfully!");

            return RedirectToAction(nameof(Assign));
        }

        // GET: ClaimsInvestigation/Details/5
        [Breadcrumb("Details", FromAction = "Draft")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        [Breadcrumb("Start New", FromAction = "Draft")]
        public async Task<IActionResult> CreatedPolicy(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = await _context.CaseLocation.FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = null,
                Location = location
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CaseReadyToAssign(ClaimTransactionModel model)
        {
            if (model == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }
            var claimsInvestigation = await _context.ClaimsInvestigation
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == model.Claim.ClaimsInvestigationId);
            claimsInvestigation.IsReady2Assign = true;
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            await _context.SaveChangesAsync();

            toastNotification.AddSuccessToastMessage("claim set ready successfully!");

            return RedirectToAction(nameof(Assign));
        }

        [Breadcrumb(title: " Detail", FromAction = "Active")]
        public async Task<IActionResult> Detail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Index")]
        public async Task<IActionResult> ReadyDetail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        [Breadcrumb(title: " Detail", FromAction = "Assign")]
        public async Task<IActionResult> AssignDetail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        [Breadcrumb(title: " Add New")]
        public async Task<IActionResult> CreatePolicy()
        {
            var userEmailToSend = string.Empty;
            var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId;
            var model = new ClaimsInvestigation { PolicyDetail = new PolicyDetail { LineOfBusinessId = lineOfBusinessId } };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (clientCompanyUser == null)
            {
                model.HasClientCompany = false;
                userEmailToSend = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin).Email;
            }
            else
            {
                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));

                var assignerUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var assignedUser in assignerUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(assignedUser, assignerRole.Name);
                    if (isTrue)
                    {
                        userEmailToSend = assignedUser.Email;
                        break;
                    }
                }

                model.PolicyDetail.ClientCompanyId = clientCompanyUser.ClientCompanyId;
            }
            ViewBag.ClientCompanyId = clientCompanyUser.ClientCompanyId;

            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name");
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claimId = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage("Policy (s) created successfully!");

            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        [Breadcrumb(title: " Edit Policy", FromAction = "Draft")]
        public async Task<IActionResult> EditPolicy(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation.Include(c => c.PolicyDetail).FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("Edit", "ClaimsInvestigation", "Edit Policy") { Parent = incompleteClaim, RouteValues = new { id = id } };

            ViewData["BreadcrumbNode"] = locationPage;

            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claimId = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage("Policy edited successfully!");

            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        [Breadcrumb(title: " Add Customer", FromAction = "Draft")]
        public async Task<IActionResult> CreateCustomer(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("CreateCustomer", "ClaimsInvestigation", "Add Customer") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;
            return View(claimsInvestigation);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claimId = await claimsInvestigationService.CreateCustomer(userEmail, claimsInvestigation, documentFile, profileFile, create);

            await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage("Policy edited successfully!");

            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        [Breadcrumb(title: " Edit Customer", FromAction = "Draft")]
        public async Task<IActionResult> EditCustomer(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("EditCustomer", "ClaimsInvestigation", "Edit Customer") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;

            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Create
        [Breadcrumb(title: " Create Claim")]
        public async Task<IActionResult> Create()
        {
            var userEmailToSend = string.Empty;
            var model = new ClaimsInvestigation { PolicyDetail = new PolicyDetail { LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId } };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (clientCompanyUser == null)
            {
                model.HasClientCompany = false;
                userEmailToSend = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin).Email;
            }
            else
            {
                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));

                var assignerUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var assignedUser in assignerUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(assignedUser, assignerRole.Name);
                    if (isTrue)
                    {
                        userEmailToSend = assignedUser.Email;
                        break;
                    }
                }

                model.PolicyDetail.ClientCompanyId = clientCompanyUser.ClientCompanyId;
            }
            ViewBag.ClientCompanyId = clientCompanyUser.ClientCompanyId;

            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name");
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        // POST: ClaimsInvestigation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            if (status == null || !ModelState.IsValid)
            {
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
                ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);
                toastNotification.AddErrorToastMessage("Error!!!");
                return View(claimsInvestigation);
            }
            IFormFile documentFile = null;
            IFormFile profileFile = null;
            var files = Request.Form?.Files;

            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail.Document?.Name);
                if (file != null)
                {
                    documentFile = file;
                }
                file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail.ProfileImage?.Name);
                if (file != null)
                {
                    profileFile = file;
                }
            }

            var claimId = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage("<i class=\"fas fa-newspaper\"></i> Claim created successfully!");

            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        // GET: ClaimsInvestigation/Edit/5
        [Breadcrumb(title: " Edit Claim", FromAction = "Draft")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation.Include(c => c.PolicyDetail).FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);

            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var subStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR));

            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            claimsInvestigation.InvestigationCaseStatusId = status.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseStatus = status;
            claimsInvestigation.InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId;
            claimsInvestigation.InvestigationCaseSubStatus = subStatus;
            claimsInvestigation.PolicyDetail.ClientCompanyId = companyUser?.ClientCompanyId;

            if (claimsInvestigationId != claimsInvestigation.ClaimsInvestigationId || !ModelState.IsValid)
            {
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);
                ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.CustomerDetail.PinCodeId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);
                toastNotification.AddErrorToastMessage("Error!!!");

                return View(claimsInvestigation);
            }
            try
            {
                IFormFile documentFile = null;
                IFormFile profileFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail.Document?.Name);
                    if (file != null)
                    {
                        documentFile = file;
                    }
                    file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail.ProfileImage?.Name);
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }

                await claimsInvestigationService.Create(userEmail, claimsInvestigation, documentFile, profileFile, false);
                toastNotification.AddSuccessToastMessage("claim case edited successfully!");
            }
            catch (Exception)
            {
                if (!ClaimsInvestigationExists(claimsInvestigation.ClaimsInvestigationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Draft));
        }

        // GET: ClaimsInvestigation/Edit/5
        [Breadcrumb(title: " Withdraw", FromAction ="Active")]
        public async Task<IActionResult> Withdraw(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = claimsInvestigation.CaseLocations.FirstOrDefault();

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            return View(model);
        }

        // POST: ClaimsInvestigation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        public async Task<IActionResult> SetWithdraw(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            if (claimsInvestigationId == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var finishedStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var withDrawnByCompanySubStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

            var existingClaimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State).AsNoTracking()
               .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId);

            existingClaimsInvestigation.InvestigationCaseSubStatus = withDrawnByCompanySubStatus;
            existingClaimsInvestigation.InvestigationCaseStatus = finishedStatus;
            existingClaimsInvestigation.PolicyDetail.Comments = claimsInvestigation.PolicyDetail.Comments;
            _context.ClaimsInvestigation.Update(existingClaimsInvestigation);

            await _context.SaveChangesAsync();

            toastNotification.AddSuccessToastMessage("claim case withdrawn successfully!");

            return RedirectToAction(nameof(ToInvestigate));
        }

        // GET: ClaimsInvestigation/Delete/5
        [Breadcrumb(title: " Delete Claim", FromAction = "Draft")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string claimsInvestigationId)
        {
            if (_context.ClaimsInvestigation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ClaimsInvestigation'  is null.");
            }
            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(claimsInvestigationId);
            if (claimsInvestigation != null)
            {
                string userEmail = HttpContext?.User?.Identity.Name;
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.Deleted = true;
                _context.ClaimsInvestigation.Update(claimsInvestigation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Draft));
        }

        [Breadcrumb(title: " Agency detail", FromAction = "Draft")]
        public async Task<IActionResult> VendorDetail(string companyId, string id, string backurl, string selectedcase)
        {
            if (id == null || _context.Vendor == null)
            {
                toastNotification.AddErrorToastMessage("agency not found!");
                return NotFound();
            }

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendor == null)
            {
                return NotFound();
            }
            ViewBag.CompanyId = companyId;
            ViewBag.Backurl = backurl;
            ViewBag.Selectedcase = selectedcase;

            return View(vendor);
        }

        public async Task<IActionResult> Uploads()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Uploads(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                string csvData = await System.IO.File.ReadAllTextAsync(filePath);
                bool firstRow = true;
                var claims = new List<ClaimsInvestigation>();
                foreach (string row in csvData.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        if (!string.IsNullOrEmpty(row))
                        {
                            if (firstRow)
                            {
                                firstRow = false;
                            }
                            else
                            {
                                var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                                var rowData = output.Split(',').ToList();
                                var claim = new ClaimsInvestigation
                                {
                                };
                                claims.Add(claim);
                            }
                        }
                    }
                }

                return View(claims);
            }
            return Problem();
        }

        private bool ClaimsInvestigationExists(string id)
        {
            return (_context.ClaimsInvestigation?.Any(e => e.ClaimsInvestigationId == id)).GetValueOrDefault();
        }
    }
}