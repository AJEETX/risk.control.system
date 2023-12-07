using CsvHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
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
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using risk.control.system.Helpers;

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
        private readonly IFtpService ftpService;
        private readonly IHttpClientService httpClientService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;
        private static HttpClient httpClient = new();

        public ClaimsInvestigationController(ApplicationDbContext context,
            IFtpService ftpService,
            IHttpClientService httpClientService,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            UserManager<ClientCompanyApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification)
        {
            _context = context;
            this.ftpService = ftpService;
            this.httpClientService = httpClientService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
            this.roleManager = roleManager;
            this.toastNotification = toastNotification;
        }

        [HttpPost]
        [Breadcrumb(" FTP")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FtpDownload()
        {
            try
            {
                var userEmail = HttpContext.User.Identity.Name;

                await ftpService.DownloadFtp(userEmail);

                toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Ftp Downloaded Claims ready"));

                return RedirectToAction("Draft");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FtpUpload(IFormFile postedFtp)
        {
            if (postedFtp != null)
            {
                string folder = Path.Combine(webHostEnvironment.WebRootPath, "document");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Path.GetFileName(postedFtp.FileName);
                string filePath = Path.Combine(folder, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFtp.CopyTo(stream);
                }
                var wc = new WebClient
                {
                    Credentials = new NetworkCredential(Applicationsettings.FTP_SITE_LOG, Applicationsettings.FTP_SITE_DATA),
                };
                var response = wc.UploadFile(Applicationsettings.FTP_SITE + fileName, filePath);

                var data = Encoding.UTF8.GetString(response);

                var userEmail = HttpContext.User.Identity.Name;

                await SaveUpload(postedFtp, filePath, "Ftp upload", userEmail);

                toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Ftp Uploaded Claims."));

                return RedirectToAction("Draft");
            }
            return Problem();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> UploadClaims(IFormFile postedFile)
        {
            if (postedFile != null && Path.GetExtension(postedFile.FileName) == ".zip")
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-file");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string docPath = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(docPath))
                {
                    Directory.CreateDirectory(docPath);
                }
                string fileName = Path.GetFileName(postedFile.FileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }
                var userEmail = HttpContext.User.Identity.Name;

                await ftpService.UploadFile(userEmail, filePath, docPath, fileNameWithoutExtension);

                //await SaveTheClaims(dataObject);
                await SaveUpload(postedFile, filePath, "File upload", userEmail);
                try
                {
                    var rows = _context.SaveChanges();
                    toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> File uploaded Claims ready"));

                    return RedirectToAction("Draft");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            }
            return Problem();
        }

        private async Task SaveUpload(IFormFile file, string filePath, string description, string uploadedBy)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var company = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == uploadedBy);
            var fileModel = new FileOnFileSystemModel
            {
                CreatedOn = DateTime.UtcNow,
                FileType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description,
                FilePath = filePath,
                UploadedBy = uploadedBy,
                CompanyId = company.ClientCompanyId
            };
            _context.FilesOnFileSystem.Add(fileModel);
            _context.SaveChanges();
        }

        [Breadcrumb(" Add New")]
        public async Task<IActionResult> CreateClaim()
        {
            var claim = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId
                }
            };

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

        [Breadcrumb(" Allocate(manual)", FromAction = "Index")]
        public IActionResult Assigner()
        {
            return View();
        }

        // GET: ClaimsInvestigation

        [Breadcrumb(" Assign", FromAction = "Index")]
        public IActionResult Draft()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb(" Empanelled Agencies", FromAction = "Assigner")]
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
                .Where(c => c.Clients.Any(c => c.ClientCompanyId == claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId))
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
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

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = url;

            var beneficiarylatLong = claimCase.PinCode.Latitude + "," + claimCase.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

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

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CaseAllocatedToVendor(string selectedcase, string claimId, long caseLocationId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            var policy = await claimsInvestigationService.AllocateToVendor(userEmail, claimId, selectedcase, caseLocationId);

            await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimId, selectedcase, caseLocationId);

            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # {0}] submitted to Agency {1} !", policy.PolicyDetail.ContractNumber, vendor.Name));

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var isCreator = await userManager.IsInRoleAsync(companyUser, AppRoles.Creator.ToString());

            //if(isCreator)
            //{
            //    return RedirectToAction(nameof(ClaimsInvestigationController.Draft), "ClaimsInvestigation");
            //}
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

        [Breadcrumb(" Assessed")]
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

        [Breadcrumb(title: "Report", FromAction = "Assessor")]
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

            if (claimCase.ClaimReport.LocationLongLat != null)
            {
                var longLat = claimCase.ClaimReport.LocationLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.LocationUrl = url;
                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(claimCase.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimCase.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                ViewBag.LocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = await httpClientService.GetAddress("-37.839542", "145.164834");
                ViewBag.LocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.LocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            if (claimCase.ClaimReport.OcrLongLat != null)
            {
                var longLat = claimCase.ClaimReport.OcrLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.OcrLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.OcrLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.OcrLocationUrl = url;
                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                ViewBag.OcrLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = await httpClientService.GetAddress("-37.839542", "145.164834");
                ViewBag.OcrLocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.OcrLocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var curl = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = curl;

            var beneficiarylatLong = claimCase.PinCode.Latitude + "," + claimCase.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

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
                .Include(l => l.Vendor)
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

            if (location.ClaimReport.LocationLongLat != null)
            {
                var longLat = location.ClaimReport.LocationLongLat.IndexOf("/");
                var latitude = location.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
                var longitude = location.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim().Replace("/", "");
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.LocationUrl = url;
                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                ViewBag.LocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = await httpClientService.GetAddress("-37.839542", "145.164834");
                ViewBag.LocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.LocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            if (location.ClaimReport.OcrLongLat != null)
            {
                var longLat = location.ClaimReport.OcrLongLat.IndexOf("/");
                var latitude = location.ClaimReport.OcrLongLat.Substring(0, longLat)?.Trim();
                var longitude = location.ClaimReport.OcrLongLat.Substring(longLat + 1)?.Trim().Replace("/", "");
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.OcrLocationUrl = url;
                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));

                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claimsInvestigation.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimsInvestigation.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(value: location.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(location.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                ViewBag.OcrLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            else
            {
                RootObject rootObject = await httpClientService.GetAddress("-37.839542", "145.164834");
                ViewBag.OcrLocationAddress = rootObject.display_name ?? "12 Heathcote Drive Forest Hill VIC 3131";
                ViewBag.OcrLocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }

            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var curl = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = curl;

            var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            var reportUpdateStatus = AssessorRemarkType.OK;

            var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # <i> {0} </i>] report submitted to Company !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ReProcessCaseReport(string assessorRemarks, string assessorRemarkType, string claimId, long caseLocationId)
        {
            string userEmail = HttpContext?.User?.Identity.Name;

            if (string.IsNullOrWhiteSpace(assessorRemarks))
            {
                assessorRemarks = "review";
            }
            var reportUpdateStatus = AssessorRemarkType.REVIEW;

            var claim = await claimsInvestigationService.ProcessCaseReport(userEmail, assessorRemarks, caseLocationId, claimId, reportUpdateStatus);

            await mailboxService.NotifyClaimReportProcess(userEmail, claimId, caseLocationId);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim [Policy # <i> {0} </i> ] investigation reassigned !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(ClaimsInvestigationController.Assessor));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                toastNotification.AddAlertToastMessage("No case selected!!!. Please select case to be assigned.");
                return RedirectToAction(nameof(Draft));
            }

            //IF AUTO ALLOCATION TRUE
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(e => e.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(e => e.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompany.ClientCompanyId);

            if (company is not null && company.Auto)
            {
                var autoAllocatedClaims = await claimsInvestigationService.ProcessAutoAllocation(claims, company, userEmail);

                if (claims.Count == autoAllocatedClaims.Count)
                {
                    toastNotification.AddSuccessToastMessage($"<i class='far fa-file-powerpoint'></i> {autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-allocated !");
                }

                if (claims.Count > autoAllocatedClaims.Count)
                {
                    if (autoAllocatedClaims.Count > 0)
                    {
                        toastNotification.AddSuccessToastMessage($"<i class='far fa-file-powerpoint'></i> {autoAllocatedClaims.Count}/{claims.Count} claim(s) auto-allocated !");
                    }

                    var notAutoAllocated = claims.Except(autoAllocatedClaims)?.ToList();

                    await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                    await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, notAutoAllocated);

                    toastNotification.AddWarningToastMessage($"<i class='far fa-file-powerpoint'></i> {notAutoAllocated.Count}/{claims.Count} claim(s) assigned successfully !");

                    return RedirectToAction(nameof(Assigner));
                }
            }
            else
            {
                await claimsInvestigationService.AssignToAssigner(HttpContext.User.Identity.Name, claims);

                await mailboxService.NotifyClaimAssignmentToAssigner(HttpContext.User.Identity.Name, claims);

                toastNotification.AddSuccessToastMessage($"<i class='far fa-file-powerpoint'></i> {claims.Count}/{claims.Count} claim(s) assigned successfully !");
            }

            return RedirectToAction(nameof(Draft));
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
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Country)
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

            if (claimsInvestigation.CustomerDetail is not null)
            {
                var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.CustomerLocationUrl = url;
            }

            if (location is not null)
            {
                var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
                var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.BeneficiaryLocationUrl = bUrl;
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
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

            toastNotification.AddSuccessToastMessage("<i class='far fa-file-powerpoint'></i> Claim details completed successfully!");

            return RedirectToAction(nameof(Draft));
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
                .ThenInclude(c => c.District)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.State)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Country)
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
            var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.CustomerLocationUrl = url;

            var beneficiarylatLong = location.PinCode.Latitude + "," + location.PinCode.Longitude;
            var bUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneficiarylatLong}&zoom=18&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{beneficiarylatLong}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            ViewBag.BeneficiaryLocationUrl = bUrl;

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
            var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId;

            var random = new Random();
            var cNumber = random.Next(3333, 9999);
            var contractNumber = "POLX" + cNumber;

            var existingContractNumber = _context.PolicyDetail.Any(p => p.ContractNumber == contractNumber);
            if (existingContractNumber)
            {
                cNumber = cNumber + random.Next(3333, 9999);
            }
            var model = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = lineOfBusinessId,
                    CaseEnablerId = _context.CaseEnabler.FirstOrDefault().CaseEnablerId,
                    CauseOfLoss = "LOST IN ACCIDENT",
                    ClaimType = ClaimType.DEATH,
                    ContractIssueDate = DateTime.UtcNow.AddDays(-10),
                    CostCentreId = _context.CostCentre.FirstOrDefault().CostCentreId,
                    DateOfIncident = DateTime.UtcNow.AddDays(-3),
                    InvestigationServiceTypeId = _context.InvestigationServiceType.FirstOrDefault(i => i.Code == "COMP").InvestigationServiceTypeId,
                    Comments = "SOMETHING FISHY",
                    SumAssuredValue = random.Next(100000, 9999999),
                    ContractNumber = "POLX" + cNumber,
                }
            };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            model.PolicyDetail.ClientCompanyId = clientCompanyUser.ClientCompanyId;

            ViewBag.ClientCompanyId = clientCompanyUser.ClientCompanyId;

            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
            i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation claimsInvestigation)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

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

            var claim = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Policy # <i><b> {0} </b> </i>  created successfully !", claim.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
        }

        [ValidateAntiForgeryToken]
        [Breadcrumb(title: " Edit Policy", FromAction = "Draft")]
        public async Task<IActionResult> EditPolicy(string id)
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
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
            i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("Edit", "ClaimsInvestigation", "Edit Policy") { Parent = incompleteClaim, RouteValues = new { id = id } };

            ViewData["BreadcrumbNode"] = locationPage;

            return View(claimsInvestigation);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

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

            var claim = await claimsInvestigationService.EdiPolicy(userEmail, claimsInvestigation, documentFile);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Policy # <i><b> {0} </b></i>  edited successfully !", claimsInvestigation.PolicyDetail.ContractNumber));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
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
            var countryId = _context.Country.FirstOrDefault().CountryId;
            var stateId = _context.State.FirstOrDefault().StateId;
            var districtId = _context.District.Include(d => d.State).FirstOrDefault(d => d.StateId == stateId).DistrictId;
            var pinCodeId = _context.PinCode.Include(p => p.District).FirstOrDefault(p => p.DistrictId == districtId).PinCodeId;
            var random = new Random();
            claimsInvestigation.CustomerDetail = new CustomerDetail
            {
                Addressline = random.Next(100, 999) + " GOOD STREET",
                ContactNumber = random.NextInt64(5555555555, 9999999999),
                CountryId = countryId,
                CustomerDateOfBirth = DateTime.Now.AddYears(-random.Next(25, 77)).AddDays(20),
                CustomerEducation = Education.PROFESSIONAL,
                CustomerIncome = Income.UPPER_INCOME,
                CustomerName = NameGenerator.GenerateName(),
                CustomerOccupation = Occupation.SELF_EMPLOYED,
                CustomerType = CustomerType.HNI,
                Description = "DODGY PERSON",
                StateId = stateId,
                DistrictId = districtId,
                PinCodeId = pinCodeId,
                Gender = Gender.MALE,
            };

            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District.OrderBy(d => d.Code), "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Code", claimsInvestigation.CustomerDetail.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State.OrderBy(s => s.Code), "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);

            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("CreateCustomer", "ClaimsInvestigation", "Add Customer") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;
            return View(claimsInvestigation);
        }

        [ValidateAntiForgeryToken]
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

            var claim = await claimsInvestigationService.CreateCustomer(userEmail, claimsInvestigation, documentFile, profileFile, create);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='fas fa-user-plus'></i> Customer {0} added successfully !", claimsInvestigation.CustomerDetail.CustomerName));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
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
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

            var country = _context.Country.Where(c => c.CountryId == claimsInvestigation.CustomerDetail.CountryId);
            var relatedStates = _context.State.Include(s => s.Country).Where(s => s.Country.CountryId == claimsInvestigation.CustomerDetail.CountryId).OrderBy(d => d.Name);
            var districts = _context.District.Include(d => d.State).Where(d => d.State.StateId == claimsInvestigation.CustomerDetail.StateId).OrderBy(d => d.Name);
            var pincodes = _context.PinCode.Include(d => d.District).Where(d => d.District.DistrictId == claimsInvestigation.CustomerDetail.DistrictId).OrderBy(d => d.Name);

            ViewData["CountryId"] = new SelectList(country.OrderBy(c => c.Name), "CountryId", "Name", claimsInvestigation.CustomerDetail.CountryId);
            ViewData["StateId"] = new SelectList(relatedStates, "StateId", "Name", claimsInvestigation.CustomerDetail.StateId);
            ViewData["DistrictId"] = new SelectList(districts, "DistrictId", "Name", claimsInvestigation.CustomerDetail.DistrictId);
            ViewData["PinCodeId"] = new SelectList(pincodes, "PinCodeId", "Code", claimsInvestigation.CustomerDetail.PinCodeId);

            var activeClaims = new MvcBreadcrumbNode("Index", "ClaimsInvestigation", "Claims");
            var incompleteClaims = new MvcBreadcrumbNode("Draft", "ClaimsInvestigation", "Draft") { Parent = activeClaims };

            var incompleteClaim = new MvcBreadcrumbNode("Details", "ClaimsInvestigation", "Details") { Parent = incompleteClaims, RouteValues = new { id = id } };

            var locationPage = new MvcBreadcrumbNode("EditCustomer", "ClaimsInvestigation", "Edit Customer") { Parent = incompleteClaim, RouteValues = new { id = id } };
            ViewData["BreadcrumbNode"] = locationPage;

            return View(claimsInvestigation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

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

            var claim = await claimsInvestigationService.EditCustomer(userEmail, claimsInvestigation, profileFile);

            //await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);

            toastNotification.AddSuccessToastMessage(string.Format("<i class='fas fa-user-check'></i> Customer {0} edited successfully !", claimsInvestigation.CustomerDetail.CustomerName));

            return RedirectToAction(nameof(Details), new { id = claim.ClaimsInvestigationId });
        }

        // GET: ClaimsInvestigation/Delete/5
        [Breadcrumb(title: " Delete", FromAction = "Draft")]
        public async Task<IActionResult> Delete(string id)
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

        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ClaimTransactionModel model)
        {
            if (model == null || _context.ClaimsInvestigation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ClaimsInvestigation'  is null.");
            }
            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.Claim.ClaimsInvestigationId);
            string userEmail = HttpContext?.User?.Identity.Name;
            claimsInvestigation.Updated = DateTime.UtcNow;
            claimsInvestigation.UpdatedBy = userEmail;
            claimsInvestigation.Deleted = true;
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Claim deleted successfully !"));
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
    }
}