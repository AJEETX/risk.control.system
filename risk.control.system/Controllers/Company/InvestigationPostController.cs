using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class InvestigationPostController : Controller
    {
        private const string CLAIMS = "claims";
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly IProcessCaseService processCaseService;
        private readonly IMailService mailboxService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationService service;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProgressService progressService;
        private readonly ILogger<InvestigationPostController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public InvestigationPostController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            ICustomApiCLient customApiCLient,
            IProcessCaseService processCaseService,
            IMailService mailboxService,
            IFtpService ftpService,
            INotyfService notifyService,
            IInvestigationService service,
            IHttpContextAccessor httpContextAccessor,
            IProgressService progressService,
            ILogger<InvestigationPostController> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            this.empanelledAgencyService = empanelledAgencyService;
            this.customApiCLient = customApiCLient;
            this.processCaseService = processCaseService;
            this.mailboxService = mailboxService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.service = service;
            this.httpContextAccessor = httpContextAccessor;
            this.progressService = progressService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(IFormFile postedFile, CreateClaims model)
        {
            try
            {
                if (postedFile == null || model == null ||
                string.IsNullOrWhiteSpace(Path.GetFileName(postedFile.FileName)) ||
                string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(postedFile.FileName))) ||
                Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip"
                )
                {
                    notifyService.Custom($"Invalid File Upload Error. ", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(CaseUploadController.Uploads), "CaseUpload");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var uploadId = await ftpService.UploadFile(currentUserEmail, postedFile, CREATEDBY.AUTO, model.UploadAndAssign);
                var jobId = backgroundJobClient.Enqueue(() => ftpService.StartFileUpload(currentUserEmail, uploadId, baseUrl, model.UploadAndAssign));
                progressService.AddUploadJob(jobId, currentUserEmail);
                if (!model.UploadAndAssign)
                {
                    notifyService.Custom($"Upload in progress ", 3, "#17A2B8", "fa fa-upload");
                }
                else
                {
                    notifyService.Custom($"Direct Assign in progress ", 5, "#dc3545", "fa fa-upload");

                }

                return RedirectToAction(nameof(CaseUploadController.Uploads), "CaseUpload", new { uploadId = uploadId });

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
                return RedirectToAction(nameof(CaseUploadController.Uploads), "CaseUpload");
            }
        }

        [HttpGet]
        public IActionResult GetJobStatus()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var jobIds = progressService.GetUploadJobIds(currentUserEmail);

            if (jobIds == null || jobIds.Count == 0)
            {
                return Json(new { jobId = "", status = "Not Found" });
            }

            using (var connection = JobStorage.Current.GetConnection())
            {
                foreach (var jobId in jobIds)
                {
                    var state = connection.GetStateData(jobId);
                    string jobStatus = state?.Name ?? "Not Found";

                    // Return first active job (Processing or Enqueued)
                    if (jobStatus == "Processing" || jobStatus == "Enqueued")
                    {
                        return Json(new { jobId, status = jobStatus });
                    }
                }
            }

            // If no active jobs are found, return the last completed job
            return Json(new { jobId = jobIds.Last(), status = "Completed or Failed" });
        }

        public IActionResult GetJobProgress(int jobId)
        {
            int progress = progressService.GetProgress(jobId);
            return Json(new { progress });
        }

        public IActionResult GetAssignmentProgress(string jobId)
        {
            int progress = progressService.GetAssignmentProgress(jobId);
            return Json(new { progress });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreatePolicy(InvestigationTask model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model == null || model.PolicyDetail == null || !model.PolicyDetail.IsValidCaseDetail())
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }
                var files = Request.Form?.Files;
                if (files == null || files.Count == 0)
                {
                    notifyService.Warning("No Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }
                var file = files.FirstOrDefault(f => f.FileName == model.PolicyDetail?.Document?.FileName && f.Name == model.PolicyDetail?.Document?.Name);
                if (file == null)
                {
                    notifyService.Warning("Invalid Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }

                var claim = await service.CreatePolicy(currentUserEmail, model, file);
                if (claim == null)
                {
                    notifyService.Error("Error Creating Case detail");
                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }
                else
                {
                    notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = claim.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditPolicy(long id, InvestigationTask model)
        {
            try
            {
                if (id < 1)
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(InvestigationController.CreatePolicy), "Investigation");
                }
                if (model == null || model.PolicyDetail == null || !model.PolicyDetail.IsValidCaseDetail())
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(InvestigationController.EditPolicy), "Investigation", new { id = id });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;

                IFormFile documentFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == model.PolicyDetail?.Document?.FileName && f.Name == model.PolicyDetail?.Document?.Name);
                    if (file != null)
                    {
                        documentFile = file;
                    }
                }

                var claim = await service.EditPolicy(currentUserEmail, model, documentFile);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error editing policy");
                    return RedirectToAction(nameof(InvestigationController.EditPolicy), "Investigation", new { id = id });
                }
                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} edited successfully", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = claim.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreateCustomer(CustomerDetail customerDetail)
        {
            try
            {
                if (customerDetail == null || customerDetail.InvestigationTaskId < 1)
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");
                    return RedirectToAction(nameof(InvestigationController.Create), "Investigation");
                }
                if (customerDetail.SelectedCountryId < 1 || customerDetail.SelectedStateId < 1 || customerDetail.SelectedDistrictId < 1 || customerDetail.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(customerDetail.Addressline) || customerDetail.Income == null || string.IsNullOrWhiteSpace(customerDetail.ContactNumber) ||
                    customerDetail.DateOfBirth == null || customerDetail.Education == null || customerDetail.Gender == null || customerDetail.Occupation == null || customerDetail.ProfileImage == null)
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");
                    return RedirectToAction(nameof(InvestigationController.CreateCustomer), "Investigation", new { id = customerDetail.InvestigationTaskId });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var files = Request.Form?.Files;
                if (files == null || files.Count == 0)
                {
                    notifyService.Warning("No Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(InvestigationController.CreateCustomer), "Investigation", new { id = customerDetail.InvestigationTaskId });

                }
                var file = files.FirstOrDefault(f => f.FileName == customerDetail?.ProfileImage?.FileName && f.Name == customerDetail?.ProfileImage?.Name);
                if (file == null)
                {
                    notifyService.Warning("Invalid Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(InvestigationController.CreateCustomer), "Investigation", new { id = customerDetail.InvestigationTaskId });

                }
                var company = await service.CreateCustomer(currentUserEmail, customerDetail, file);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(InvestigationController.CreateCustomer), "Investigation", new { id = customerDetail.InvestigationTaskId });

                }
                notifyService.Custom($"Customer {customerDetail.Name} added successfully", 3, "green", "fas fa-user-plus");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = customerDetail.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(long investigationTaskId, CustomerDetail customerDetail)
        {
            try
            {
                if (customerDetail == null || investigationTaskId < 1)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(InvestigationController.Create), "Investigation");
                }
                if (customerDetail.SelectedCountryId < 1 || customerDetail.SelectedStateId < 1 || customerDetail.SelectedDistrictId < 1 || customerDetail.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(customerDetail.Addressline) || customerDetail.Income == null || string.IsNullOrWhiteSpace(customerDetail.ContactNumber) ||
                    customerDetail.DateOfBirth == null || customerDetail.Education == null || customerDetail.Gender == null || customerDetail.Occupation == null)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = investigationTaskId });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                IFormFile profileFile = null;
                var files = Request.Form?.Files;
                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == customerDetail?.ProfileImage?.FileName && f.Name == customerDetail?.ProfileImage?.Name);
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }

                var company = await service.EditCustomer(currentUserEmail, customerDetail, profileFile);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error edting customer");
                    return RedirectToAction(nameof(InvestigationController.EditCustomer), "Investigation", new { id = investigationTaskId });
                }
                notifyService.Custom($"Customer {customerDetail.Name} edited successfully", 3, "orange", "fas fa-user-plus");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = customerDetail.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBeneficiary(long investigationTaskId, BeneficiaryDetail beneficiary)
        {
            try
            {
                if (investigationTaskId < 1)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(InvestigationController.Create), "Investigation");
                }
                if (beneficiary == null || beneficiary.SelectedCountryId < 1 || beneficiary.SelectedStateId < 1 || beneficiary.SelectedDistrictId < 1 || beneficiary.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(beneficiary.Addressline) || beneficiary.BeneficiaryRelationId < 1 || string.IsNullOrWhiteSpace(beneficiary.ContactNumber) ||
                    beneficiary.DateOfBirth == null || beneficiary.Income == null || beneficiary.ProfileImage == null)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var files = Request.Form?.Files;
                if (files == null || files.Count == 0)
                {
                    notifyService.Warning("No Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
                }

                var file = files.FirstOrDefault(f => f.FileName == beneficiary?.ProfileImage?.FileName && f.Name == beneficiary?.ProfileImage?.Name);
                if (file == null)
                {
                    notifyService.Warning("Invalid Image Error !!! ");
                    return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
                }
                var company = await service.CreateBeneficiary(currentUserEmail, investigationTaskId, beneficiary, file);
                if (company == null)
                {
                    notifyService.Warning("Error creating Beneficiary !!! ");
                    return RedirectToAction(nameof(InvestigationController.CreateBeneficiary), "Investigation", new { id = beneficiary.InvestigationTaskId });
                }
                notifyService.Custom($"Beneficiary {beneficiary.Name} added successfully", 3, "green", "fas fa-user-tie");
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditBeneficiary(long beneficiaryDetailId, BeneficiaryDetail beneficiary)
        {
            try
            {
                if (beneficiaryDetailId < 0 || beneficiary == null || beneficiary.InvestigationTaskId < 1)
                {
                    notifyService.Error("OOPs !!!..Error editing customer");
                    return RedirectToAction(nameof(InvestigationController.Create), "Investigation");
                }
                if (beneficiary.SelectedCountryId < 1 || beneficiary.SelectedStateId < 1 || beneficiary.SelectedDistrictId < 1 || beneficiary.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(beneficiary.Addressline) || beneficiary.BeneficiaryRelationId < 1 || string.IsNullOrWhiteSpace(beneficiary.ContactNumber) ||
                    beneficiary.DateOfBirth == null || beneficiary.Income == null)
                {
                    notifyService.Error("OOPs !!!..Error editing customer");
                    return RedirectToAction(nameof(InvestigationController.Create), "Investigation");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                IFormFile profileFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == beneficiary?.ProfileImage?.FileName && f.Name == beneficiary?.ProfileImage?.Name);
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }
                var company = await service.EditBeneficiary(currentUserEmail, beneficiaryDetailId, beneficiary, profileFile);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error editing customer");

                }
                if (company != null)
                {
                    notifyService.Custom($"Beneficiary {beneficiary.Name} edited successfully", 3, "orange", "fas fa-user-tie");
                }
                return RedirectToAction(nameof(InvestigationController.Details), "Investigation", new { id = beneficiary.InvestigationTaskId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);

                if (id <= 0)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var claimsInvestigation = await _context.Investigations.FindAsync(id);
                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.Investigations.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Case deleted successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS!!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost, ActionName("DeleteCases")]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> DeleteCases([FromBody] DeleteRequestModel request)
        {
            if (request.claims == null || request.claims.Count == 0)
            {
                return Json(new { success = false, message = "No cases selected for deletion." });
            }

            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                foreach (var claim in request.claims)
                {
                    var claimsInvestigation = await _context.Investigations.FindAsync(claim);
                    if (claimsInvestigation == null)
                    {
                        notifyService.Error("Not Found!!!..Contact Admin");
                        return RedirectToAction(nameof(Index), "Dashboard");
                    }

                    claimsInvestigation.Updated = DateTime.Now;
                    claimsInvestigation.UpdatedBy = currentUserEmail;
                    claimsInvestigation.Deleted = true;
                    _context.Investigations.Update(claimsInvestigation);
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuto(List<long> claims)
        {

            try
            {
                if (claims == null || claims.Count == 0)
                {
                    notifyService.Custom($"No Case selected!!!. Please select Case to be assigned.", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(InvestigationController.New), "Investigation");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                // AUTO ALLOCATION COUNT
                var distinctClaims = claims.Distinct().ToList();
                var affectedRows = await processCaseService.UpdateCaseAllocationStatus(currentUserEmail, distinctClaims);
                if (affectedRows < distinctClaims.Count)
                {
                    notifyService.Custom($"Case(s) assignment error", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(InvestigationController.New), "Investigation");
                }
                var jobId = backgroundJobClient.Enqueue(() => processCaseService.BackgroundAutoAllocation(distinctClaims, currentUserEmail, baseUrl));
                progressService.AddAssignmentJob(jobId, currentUserEmail);
                notifyService.Custom($"Assignment of {distinctClaims.Count} Case(s) started", 3, "orange", "far fa-file-powerpoint");
                return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive", new { jobId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
            }
            return RedirectToAction(nameof(InvestigationController.New), "Investigation");
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> AllocateSingle2Vendor(long selectedcase, long caseId)
        {
            if (selectedcase < 1 || caseId < 1)
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(InvestigationController.New), "Investigation");
            }
            //set claim as manual assigned
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var (policy, status) = await processCaseService.AllocateToVendor(currentUserEmail, caseId, selectedcase, false);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(InvestigationController.New), "Investigation");
                }

                var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == selectedcase);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAllocationToVendorAndManager(currentUserEmail, policy, caseId, selectedcase, baseUrl));

                notifyService.Custom($"Case #{policy} {status} to {vendor.Name}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(InvestigationController.New), "Investigation");
            }
        }
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAutoSingle(long claims)
        {
            if (claims < 1)
            {
                notifyService.Custom($"No case selected!!!. Please select case to be assigned.", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(InvestigationController.New), "Investigation");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var allocatedCaseNumber = await processCaseService.ProcessAutoSingleAllocation(claims, currentUserEmail, baseUrl);
                if (string.IsNullOrWhiteSpace(allocatedCaseNumber))
                {
                    notifyService.Custom($"Case #:{allocatedCaseNumber} Not Assigned", 3, "orange", "far fa-file-powerpoint");
                    return RedirectToAction(nameof(InvestigationController.New), "Investigation");
                }
                notifyService.Custom($"Case #:{allocatedCaseNumber} Assigned", 3, "green", "far fa-file-powerpoint");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(InvestigationController.New), "Investigation");
            }
            return RedirectToAction(nameof(CaseActiveController.Active), "CaseActive");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> WithdrawCase(CaseTransactionModel model, long claimId, string policyNumber)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var (company, vendorId) = await processCaseService.WithdrawCaseByCompany(currentUserEmail, model, claimId);
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId, vendorId, baseUrl));
                //await mailboxService.NotifyClaimWithdrawlToCompany(currentUserEmail, claimId);

                notifyService.Custom($"Case #{policyNumber}  withdrawn successfully", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(InvestigationController.New), "Investigation");

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(InvestigationController.New), "Investigation");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> ProcessCaseReport(string assessorRemarks, string assessorRemarkType, long claimId, string reportAiSummary)
        {
            if (string.IsNullOrWhiteSpace(assessorRemarks) || claimId < 1 || string.IsNullOrWhiteSpace(assessorRemarkType))
            {
                notifyService.Custom($"Error!!! Try again", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                AssessorRemarkType reportUpdateStatus = (AssessorRemarkType)Enum.Parse(typeof(AssessorRemarkType), assessorRemarkType, true);

                var (company, contract) = await processCaseService.ProcessCaseReport(currentUserEmail, assessorRemarks, claimId, reportUpdateStatus, reportAiSummary);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";


                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportProcess(currentUserEmail, claimId, baseUrl));

                if (reportUpdateStatus == AssessorRemarkType.OK)
                {
                    notifyService.Custom($"Case #{contract} Approved", 3, "green", "far fa-file-powerpoint");
                }
                else if (reportUpdateStatus == AssessorRemarkType.REJECT)
                {
                    notifyService.Custom($"Case #{contract} Rejected", 3, "red", "far fa-file-powerpoint");
                }
                else
                {
                    notifyService.Custom($"Case #{contract} Re-Assigned", 3, "yellow", "far fa-file-powerpoint");
                }

                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> SubmitQuery(long claimId, string reply, CaseInvestigationVendorsModel request)
        {
            try
            {
                if (request == null || claimId < 1 || string.IsNullOrWhiteSpace(reply))
                {
                    notifyService.Error("Bad Request..");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                request.InvestigationReport.EnquiryRequest.DescriptiveQuestion = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveQuestion);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

                var model = await processCaseService.SubmitQueryToAgency(currentUserEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, messageDocument);
                if (model != null)
                {
                    var company = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitQueryToAgency(currentUserEmail, claimId, baseUrl));

                    notifyService.Success("Query Sent to Agency");
                    return RedirectToAction(nameof(AssessorController.Assessor), "Assessor");
                }
                notifyService.Error("OOPs !!!..Error sending query");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> SubmitNotes(long claimId, string name)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var model = await processCaseService.SubmitNotes(currentUserEmail, claimId, name);
                if (model)
                {
                    notifyService.Success("Notes added");
                    return Ok();
                }
                notifyService.Error("OOPs !!!..Error adding notes");
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return Ok();
            }
        }
        public class DeleteRequestModel
        {
            public List<long> claims { get; set; }
        }
    }
}
