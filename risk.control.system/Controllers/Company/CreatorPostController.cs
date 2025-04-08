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
using Hangfire;
using Amazon.Textract;
using Microsoft.AspNetCore.Http;

namespace risk.control.system.Controllers.Company
{
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CreatorPostController : Controller
    {
        private const string CLAIMS = "claims";
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly IClaimCreationService creationService;
        private readonly ICreatorService creatorService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IProgressService progressService;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IClaimPolicyService claimPolicyService;

        public CreatorPostController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            ICustomApiCLient customApiCLient,
            IClaimCreationService creationService,
            ICreatorService creatorService,
            IFtpService ftpService,
            INotyfService notifyService,
            IHttpContextAccessor httpContextAccessor,
            IInvestigationReportService investigationReportService,
            IProgressService progressService,
            IBackgroundJobClient backgroundJobClient,
            IClaimPolicyService claimPolicyService)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.customApiCLient = customApiCLient;
            this.creationService = creationService;
            this.creatorService = creatorService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.httpContextAccessor = httpContextAccessor;
            this.investigationReportService = investigationReportService;
            this.progressService = progressService;
            this.backgroundJobClient = backgroundJobClient;
        }
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
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
                    return RedirectToAction(nameof(ClaimsLogController.Uploads), "ClaimsLog");
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var uploadId = await ftpService.UploadFile(currentUserEmail, postedFile, model.CREATEDBY);
                var jobId = backgroundJobClient.Enqueue(() => ftpService.StartUpload(currentUserEmail, uploadId, baseUrl));
                progressService.AddUploadJob(jobId, currentUserEmail);

                notifyService.Custom($"Upload in progress ", 3, "orange", "fa fa-upload");

                return RedirectToAction(nameof(ClaimsLogController.Uploads), "ClaimsLog", new { uploadId = uploadId });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
                return RedirectToAction(nameof(ClaimsLogController.Uploads), "ClaimsLog");
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
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                if (model == null || model.PolicyDetail == null || model.PolicyDetail.LineOfBusinessId == 0 || model.PolicyDetail.InvestigationServiceTypeId == 0 || model.PolicyDetail.CostCentreId == 0 ||
                     string.IsNullOrWhiteSpace(model.PolicyDetail.ContractNumber) || model.PolicyDetail.CaseEnablerId == 0 || string.IsNullOrWhiteSpace(model.PolicyDetail.CauseOfLoss) ||
                     model.PolicyDetail.SumAssuredValue < 1 || model.PolicyDetail.DateOfIncident == null || model.PolicyDetail.ContractIssueDate == null || model.PolicyDetail?.Document == null && !model.PolicyDetail.IsValidPolicy())
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(CreatorAutoController.CreatePolicy), "CreatorAuto");
                }
                var files = Request.Form?.Files;
                if (files == null || files.Count == 0)
                {
                    notifyService.Warning("No Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.CreatePolicy), "CreatorAuto");
                }
                var file = files.FirstOrDefault(f => f.FileName == model.PolicyDetail?.Document?.FileName && f.Name == model.PolicyDetail?.Document?.Name);
                if (file == null)
                {
                    notifyService.Warning("Invalid Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.CreatePolicy), "CreatorAuto");
                }

                var claim = await creationService.CreatePolicy(currentUserEmail, model, file);
                if (claim == null)
                {
                    notifyService.Error("Error Creating Case detail");
                    if (claim.ClientCompany.AutoAllocation)
                        if (model.CREATEDBY == CREATEDBY.AUTO)
                        {
                            return RedirectToAction(nameof(CreatorAutoController.CreatePolicy), "CreatorAuto");
                        }
                    else
                        {
                        return RedirectToAction(nameof(CreatorManualController.Create), "CreatorManual");
                        }
                    else
                    {
                        return RedirectToAction(nameof(CreatorManualController.Create), "CreatorManual");
                    }
                }
                else
                {
                    notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");
                }

                if (claim.ClientCompany.AutoAllocation)
                {
                    if (model.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });
                    }
                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [HttpPost]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(claimsInvestigationId))
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(CreatorAutoController.CreatePolicy), "CreatorAuto");
                }
                if (model == null || model.PolicyDetail == null || model.PolicyDetail.LineOfBusinessId == 0 || model.PolicyDetail.InvestigationServiceTypeId == 0 || model.PolicyDetail.CostCentreId == 0 ||
                    string.IsNullOrWhiteSpace(model.PolicyDetail.ContractNumber) || model.PolicyDetail.CaseEnablerId == 0 || string.IsNullOrWhiteSpace(model.PolicyDetail.CauseOfLoss) ||
                    model.PolicyDetail.SumAssuredValue < 1 || model.PolicyDetail.DateOfIncident == null || model.PolicyDetail.ContractIssueDate == null &&  !model.PolicyDetail.IsValidPolicy())
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");

                    return RedirectToAction(nameof(CreatorAutoController.EditPolicy), "CreatorAuto",new {id = claimsInvestigationId });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                
                IFormFile documentFile = null;
                IFormFile profileFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == model.PolicyDetail?.Document?.FileName && f.Name == model.PolicyDetail?.Document?.Name);
                    if (file != null)
                    {
                        documentFile = file;
                    }
                }

                var claim = await creationService.EdiPolicy(currentUserEmail, model, documentFile);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error editing policy");
                    return RedirectToAction(nameof(CreatorAutoController.EditPolicy), "CreatorAuto", new { id  = claimsInvestigationId });
                }
                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} edited successfully", 3, "orange", "far fa-file-powerpoint");
                
                if (claim.ClientCompany.AutoAllocation)
                {
                    if (model.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        ViewBag.ActiveTab = "manual";
                        return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });
                    }

                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreateCustomer(CustomerDetail customerDetail)
        {
            try
            {
                if (customerDetail == null || string.IsNullOrWhiteSpace(customerDetail.ClaimsInvestigationId))
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");
                    return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto");
                }
                if (customerDetail.SelectedCountryId < 1 || customerDetail.SelectedStateId < 1 || customerDetail.SelectedDistrictId < 1 || customerDetail.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(customerDetail.Addressline) || customerDetail.Income == null || string.IsNullOrWhiteSpace(customerDetail.ContactNumber) ||
                    customerDetail.DateOfBirth == null || customerDetail.Education == null || customerDetail.Gender == null || customerDetail.Occupation == null || customerDetail.ProfileImage == null)
                {
                    notifyService.Error("OOPs !!!..Incomplete/Invalid input");
                    return RedirectToAction(nameof(CreatorAutoController.CreateCustomer), "CreatorAuto",new {id = customerDetail.ClaimsInvestigationId });
                }

                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var files = Request.Form?.Files;
                if (files == null || files.Count == 0)
                {
                    notifyService.Warning("No Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.CreateCustomer), "CreatorAuto", new { id = customerDetail.ClaimsInvestigationId });

                }
                var file = files.FirstOrDefault(f => f.FileName == customerDetail?.ProfileImage?.FileName && f.Name == customerDetail?.ProfileImage?.Name);
                if (file == null)
                {
                    notifyService.Warning("Invalid Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.CreateCustomer), "CreatorAuto", new { id = customerDetail.ClaimsInvestigationId });
                }
                var company = await creationService.CreateCustomer(currentUserEmail, customerDetail, file);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(CreatorAutoController.CreateCustomer), "CreatorAuto", new { id = customerDetail.ClaimsInvestigationId });
                }
                notifyService.Custom($"Customer {customerDetail.Name} added successfully", 3, "green", "fas fa-user-plus");
                if(company.AutoAllocation)
                {
                    if (customerDetail.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = customerDetail.ClaimsInvestigationId });
                    }
                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(string claimsInvestigationId, CustomerDetail customerDetail)
        {
            try
            {
                if(customerDetail == null || string.IsNullOrWhiteSpace(claimsInvestigationId))
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto");
                }
                if (customerDetail.SelectedCountryId < 1 || customerDetail.SelectedStateId < 1 || customerDetail.SelectedDistrictId < 1 || customerDetail.SelectedPincodeId < 1 || 
                    string.IsNullOrWhiteSpace(customerDetail.Addressline) || customerDetail.Income == null || string.IsNullOrWhiteSpace(customerDetail.ContactNumber)  ||
                    customerDetail.DateOfBirth == null || customerDetail.Education == null || customerDetail.Gender == null || customerDetail.Occupation == null)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claimsInvestigationId });
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

                var company = await creationService.EditCustomer(currentUserEmail, customerDetail, profileFile);
                if (company == null)
                {
                    notifyService.Error("OOPs !!!..Error edting customer");
                    return RedirectToAction(nameof(CreatorAutoController.EditCustomer), "CreatorAuto", new { id = claimsInvestigationId });
                }
                notifyService.Custom($"Customer {customerDetail.Name} edited successfully", 3, "orange", "fas fa-user-plus");
                if ( company.AutoAllocation)
                {
                    if (customerDetail.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = customerDetail.ClaimsInvestigationId });
                    }
                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBeneficiary(string ClaimsInvestigationId, BeneficiaryDetail beneficiary)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ClaimsInvestigationId))
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto");
                }
                if (beneficiary == null || beneficiary.SelectedCountryId < 1 || beneficiary.SelectedStateId < 1 || beneficiary.SelectedDistrictId < 1 || beneficiary.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(beneficiary.Addressline) || beneficiary.BeneficiaryRelationId < 1 || string.IsNullOrWhiteSpace(beneficiary.ContactNumber) ||
                    beneficiary.DateOfBirth == null || beneficiary.Income == null || beneficiary.ProfileImage == null)
                {
                    notifyService.Error("OOPs !!!..Error creating customer");
                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = beneficiary.ClaimsInvestigationId });
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                var files = Request.Form?.Files;
                if (files == null || files.Count == 0)
                {
                    notifyService.Warning("No Image Uploaded Error !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = beneficiary.ClaimsInvestigationId });
                }

                var file = files.FirstOrDefault(f => f.FileName == beneficiary?.ProfileImage?.FileName && f.Name == beneficiary?.ProfileImage?.Name);
                if (file == null)
                {
                    notifyService.Warning("Invalid Image Error !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = beneficiary.ClaimsInvestigationId });
                }
                var company = await creationService.CreateBeneficiary(currentUserEmail, ClaimsInvestigationId, beneficiary, file);
                if(company == null)
                {
                    notifyService.Warning("Error creating Beneficiary !!! ");
                    return RedirectToAction(nameof(CreatorAutoController.CreateBeneficiary), "CreatorAuto", new { id = beneficiary.ClaimsInvestigationId });
                }
                notifyService.Custom($"Beneficiary {beneficiary.Name} added successfully", 3, "green", "fas fa-user-tie");


                if (company.AutoAllocation)
                {
                    if (beneficiary.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = beneficiary.ClaimsInvestigationId });
                    }
                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> EditBeneficiary(long beneficiaryDetailId, BeneficiaryDetail beneficiary)
        {
            try
            {
                if (beneficiaryDetailId < 0 || beneficiary == null || string.IsNullOrWhiteSpace(beneficiary.ClaimsInvestigationId))
                {
                    notifyService.Error("OOPs !!!..Error editing customer");
                    return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto");
                }
                if (beneficiary.SelectedCountryId < 1 || beneficiary.SelectedStateId < 1 || beneficiary.SelectedDistrictId < 1 || beneficiary.SelectedPincodeId < 1 ||
                    string.IsNullOrWhiteSpace(beneficiary.Addressline) || beneficiary.BeneficiaryRelationId < 1 || string.IsNullOrWhiteSpace(beneficiary.ContactNumber) ||
                    beneficiary.DateOfBirth == null || beneficiary.Income == null)
                {
                    notifyService.Error("OOPs !!!..Error editing customer");
                    return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto");
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
                var company = await creationService.EditBeneficiary(currentUserEmail, beneficiaryDetailId, beneficiary, profileFile);
                if(company == null)
                {
                    notifyService.Error("OOPs !!!..Error editing customer");

                }
                if (company != null)
                {
                    notifyService.Custom($"Beneficiary {beneficiary.Name} edited successfully", 3, "orange", "fas fa-user-tie");
                }

                if (company.AutoAllocation)
                {
                    if (beneficiary.CREATEDBY == CREATEDBY.MANUAL)
                    {
                        return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
                    }
                    else
                    {
                        return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = beneficiary.ClaimsInvestigationId });
                    }
                }
                else
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(ClaimTransactionModel model)
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
                var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.ClaimsInvestigation.ClaimsInvestigationId);
                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.ClaimsInvestigation.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                notifyService.Custom("Claim deleted", 3, "red", "far fa-file-powerpoint");
                if(companyUser.ClientCompany.AutoAllocation)
                {
                    return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
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
                    var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(claim);
                    if (claimsInvestigation == null)
                    {
                        notifyService.Error("Not Found!!!..Contact Admin");
                        return RedirectToAction(nameof(Index), "Dashboard");
                    }

                    claimsInvestigation.Updated = DateTime.Now;
                    claimsInvestigation.UpdatedBy = currentUserEmail;
                    claimsInvestigation.Deleted = true;
                    _context.ClaimsInvestigation.Update(claimsInvestigation);
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class DeleteRequestModel
        {
            public List<string> claims { get; set; }
        }

    }
}
