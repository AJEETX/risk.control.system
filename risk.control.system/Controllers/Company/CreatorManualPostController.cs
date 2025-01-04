//using AspNetCoreHero.ToastNotification.Abstractions;
//using AspNetCoreHero.ToastNotification.Notyf;

//using Google.Api;

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;

//using risk.control.system.AppConstant;
//using risk.control.system.Data;
//using risk.control.system.Helpers;
//using risk.control.system.Models;
//using risk.control.system.Models.ViewModel;
//using risk.control.system.Services;

//using SmartBreadcrumbs.Attributes;
//using SmartBreadcrumbs.Nodes;

//using static risk.control.system.AppConstant.Applicationsettings;

//namespace risk.control.system.Controllers.Company
//{
//    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
//    public class CreatorManualPostController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IEmpanelledAgencyService empanelledAgencyService;
//        private readonly ICreatorService creatorService;
//        private readonly ICustomApiCLient customApiCLient;
//        private readonly IFtpService ftpService;
//        private readonly INotyfService notifyService;
//        private readonly IClaimCreationService creationService;
//        private readonly IInvestigationReportService investigationReportService;
//        private readonly IClaimPolicyService claimPolicyService;

//        public CreatorManualPostController(ApplicationDbContext context,
//            IEmpanelledAgencyService empanelledAgencyService,
//            ICreatorService creatorService,
//            ICustomApiCLient customApiCLient,
//            IFtpService ftpService,
//            INotyfService notifyService,
//            IClaimCreationService creationService,
//            IInvestigationReportService investigationReportService,
//            IClaimPolicyService claimPolicyService)
//        {
//            _context = context;
//            this.claimPolicyService = claimPolicyService;
//            this.empanelledAgencyService = empanelledAgencyService;
//            this.creatorService = creatorService;
//            this.customApiCLient = customApiCLient;
//            this.ftpService = ftpService;
//            this.notifyService = notifyService;
//            this.creationService = creationService;
//            this.investigationReportService = investigationReportService;
//        }

//        [HttpPost]
//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> New(IFormFile postedFile, string uploadtype, string uploadingway)
//        {
//            try
//            {
//                var currentUserEmail = HttpContext.User?.Identity?.Name;
//                if (string.IsNullOrWhiteSpace(currentUserEmail))
//                {
//                    notifyService.Error("OOPs !!!..Unauthenticated Access");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                object _;
//                if (!Enum.TryParse(typeof(UploadType), uploadtype, true, out _))
//                {
//                    notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");
//                    return RedirectToAction("New", "CreatorManual");
//                }
//                if (postedFile == null || string.IsNullOrWhiteSpace(uploadtype) ||
//                string.IsNullOrWhiteSpace(Path.GetFileName(postedFile.FileName)) ||
//                string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(postedFile.FileName))) ||
//                Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip"
//                )
//                {
//                    notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");

//                    return RedirectToAction("New", "CreatorManual");
//                }

//                if (postedFile != null)
//                {
//                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

//                    if (uploadType == UploadType.FTP)
//                    {
//                        var processed = await ftpService.UploadFtpFile(currentUserEmail, postedFile, uploadingway);
//                        if (processed)
//                        {
//                            notifyService.Custom($"FTP download complete ", 3, "green", "fa fa-upload");
//                        }
//                        else
//                        {
//                            notifyService.Information($"FTP Upload Error. Check limit <i class='fa fa-upload' ></i>", 3);
//                        }
//                    }

//                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
//                    {

//                        var processed = await ftpService.UploadFile(currentUserEmail, postedFile, uploadingway);
//                        if (processed)
//                        {
//                            notifyService.Custom($"File upload complete", 3, "green", "fa fa-upload");
//                        }
//                        else
//                        {
//                            notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
//                        }

//                    }
//                    return RedirectToAction("New", "CreatorManual");
//                }
//                notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
//                return RedirectToAction("New", "CreatorManual");
//            }
//            catch (Exception ex)
//            {
//                notifyService.Error("OOPs !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [HttpPost]
//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation claimsInvestigation)
//        {
//            try
//            {
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                if (claimsInvestigation == null)
//                {
//                    notifyService.Error("OOPs !!!..Claim Not Found");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }

//                claimsInvestigation.CREATEDBY = CREATEDBY.MANUAL;
//                claimsInvestigation.ORIGIN = ORIGIN.USER;
//                IFormFile documentFile = null;
//                IFormFile profileFile = null;
//                var files = Request.Form?.Files;

//                if (files != null && files.Count > 0)
//                {
//                    var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
//                    if (file != null && file.Length > 2000000)
//                    {
//                        notifyService.Warning("Uploaded File size morer than 2MB !!! ");
//                        return RedirectToAction(nameof(CreatorManualController.CreatePolicy), "CreatorManual", new { claimsInvestigation });
//                    }
//                    if (file != null)
//                    {
//                        documentFile = file;
//                    }
//                    file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
//                    if (file != null)
//                    {
//                        profileFile = file;
//                    }
//                }

//                var claim = await creationService.CreatePolicy(currentUserEmail, claimsInvestigation, documentFile, profileFile, false);
//                if (claim == null)
//                {
//                    notifyService.Error("OOPs !!!..Error creating policy");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");

//                return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPs !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [ValidateAntiForgeryToken]
//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        [HttpPost]
//        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, string claimtype)
//        {
//            try
//            {
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                if (claimsInvestigation == null || string.IsNullOrWhiteSpace(claimsInvestigationId) || claimtype == null)
//                {
//                    notifyService.Error("OOPs !!!..Claim Not found");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }

//                IFormFile documentFile = null;
//                IFormFile profileFile = null;
//                var files = Request.Form?.Files;

//                if (files != null && files.Count > 0)
//                {
//                    var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
//                    if (file != null)
//                    {
//                        documentFile = file;
//                    }
//                    file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
//                    if (file != null)
//                    {
//                        profileFile = file;
//                    }
//                }

//                var claim = await creationService.EdiPolicy(currentUserEmail, claimsInvestigation, documentFile);
//                if (claim == null)
//                {
//                    notifyService.Error("OOPs !!!..Error editing policy");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} edited successfully", 3, "orange", "far fa-file-powerpoint");
//                if (string.IsNullOrWhiteSpace(claimtype) || claimtype.Equals("draft", StringComparison.OrdinalIgnoreCase))
//                {
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "ClaimsInvestigation", new { id = claim.ClaimsInvestigationId });
//                }

//                else if (claimtype.Equals("auto", StringComparison.OrdinalIgnoreCase))
//                {
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });

//                }
//                else if (claimtype.Equals("manual", StringComparison.OrdinalIgnoreCase))
//                {
//                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
//                }
//                return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPs !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [ValidateAntiForgeryToken]
//        [HttpPost]
//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        public async Task<IActionResult> CreateCustomer(CustomerDetail customerDetail)
//        {
//            try
//            {
//                if (customerDetail == null || customerDetail.SelectedCountryId < 1 || customerDetail.SelectedStateId < 1 || customerDetail.SelectedDistrictId < 1 || customerDetail.SelectedPincodeId < 1)
//                {
//                    notifyService.Error("OOPs !!!..Error creating customer");
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
//                }
//                var currentUserEmail = HttpContext.User?.Identity?.Name;
//                IFormFile profileFile = null;
//                var files = Request.Form?.Files;
//                if (files != null && files.Count > 0)
//                {
//                    var file = files.FirstOrDefault(f => f.FileName == customerDetail?.ProfileImage?.FileName && f.Name == customerDetail?.ProfileImage?.Name);
//                    if (file != null)
//                    {
//                        profileFile = file;
//                    }
//                }

//                var claim = await creationService.CreateCustomer(currentUserEmail, customerDetail, profileFile);
//                if (!claim)
//                {
//                    notifyService.Error("OOPs !!!..Error creating customer");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                notifyService.Custom($"Customer {customerDetail.Name} added successfully", 3, "green", "fas fa-user-plus");

//                return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPs !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        [ValidateAntiForgeryToken]
//        [HttpPost]
//        public async Task<IActionResult> EditCustomer(string claimsInvestigationId, CustomerDetail customerDetail, string claimtype, bool create = true)
//        {
//            try
//            {
//                if (customerDetail == null || customerDetail.SelectedCountryId < 1 || customerDetail.SelectedStateId < 1 || customerDetail.SelectedDistrictId < 1 || customerDetail.SelectedPincodeId < 1)
//                {
//                    notifyService.Error("OOPs !!!..Error creating customer");
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorManual", new { id = customerDetail.ClaimsInvestigationId });
//                }
//                var currentUserEmail = HttpContext.User?.Identity?.Name;
//                IFormFile profileFile = null;
//                var files = Request.Form?.Files;

//                if (files != null && files.Count > 0)
//                {
//                    var file = files.FirstOrDefault(f => f.FileName == customerDetail?.ProfileImage?.FileName && f.Name == customerDetail?.ProfileImage?.Name);
//                    if (file != null)
//                    {
//                        profileFile = file;
//                    }
//                }

//                var claim = await creationService.EditCustomer(currentUserEmail, customerDetail, profileFile);
//                if (!claim)
//                {
//                    notifyService.Error("OOPs !!!..Error edting customer");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                notifyService.Custom($"Customer {customerDetail.Name} edited successfully", 3, "orange", "fas fa-user-plus");
//                if (claimtype.Equals("auto", StringComparison.OrdinalIgnoreCase))
//                {
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claimsInvestigationId });

//                }
//                else if (claimtype.Equals("manual", StringComparison.OrdinalIgnoreCase))
//                {
//                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claimsInvestigationId });

//                }
//                return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claimsInvestigationId });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPs !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [HttpPost]
//        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CreateBeneficiary(string ClaimsInvestigationId, BeneficiaryDetail beneficiary)
//        {
//            try
//            {
//                if (beneficiary == null || beneficiary.SelectedCountryId < 1 || beneficiary.SelectedStateId < 1 || beneficiary.SelectedDistrictId < 1 || beneficiary.SelectedPincodeId < 1)
//                {
//                    notifyService.Error("OOPs !!!..Error creating customer");
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
//                }
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                if (string.IsNullOrWhiteSpace(ClaimsInvestigationId) || beneficiary is null)
//                {
//                    notifyService.Error("NOT FOUND  !!!..Claim Not Found");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                IFormFile profileFile = null;
//                var files = Request.Form?.Files;

//                if (files != null && files.Count > 0)
//                {
//                    var file = files.FirstOrDefault(f => f.FileName == beneficiary?.ProfileImage?.FileName && f.Name == beneficiary?.ProfileImage?.Name);
//                    if (file != null)
//                    {
//                        profileFile = file;
//                    }
//                }
//                var created = await creationService.CreateBeneficiary(currentUserEmail, ClaimsInvestigationId, beneficiary, profileFile);

//                if (created)
//                {
//                    notifyService.Custom($"Beneficiary {beneficiary.Name} added successfully", 3, "green", "fas fa-user-tie");

//                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = ClaimsInvestigationId });
//                }
//                notifyService.Error("OOPS !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPS !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [RequestSizeLimit(2_000_000)] // Limit request size to 2 MB
//        public async Task<IActionResult> EditBeneficiary(long beneficiaryDetailId, BeneficiaryDetail beneficiary)
//        {
//            try
//            {
//                if (beneficiary == null || beneficiary.SelectedCountryId < 1 || beneficiary.SelectedStateId < 1 || beneficiary.SelectedDistrictId < 1 || beneficiary.SelectedPincodeId < 1)
//                {
//                    notifyService.Error("OOPs !!!..Error creating customer");
//                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
//                }
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                IFormFile profileFile = null;
//                var files = Request.Form?.Files;

//                if (files != null && files.Count > 0)
//                {
//                    var file = files.FirstOrDefault(f => f.FileName == beneficiary?.ProfileImage?.FileName && f.Name == beneficiary?.ProfileImage?.Name);
//                    if (file != null)
//                    {
//                        profileFile = file;
//                    }
//                }
//                var created = await creationService.EditBeneficiary(currentUserEmail, beneficiaryDetailId, beneficiary, profileFile);
//                if (!created)
//                {
//                    notifyService.Error("OOPS !!!..Contact Admin");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }

//                Notify success and redirect
//                notifyService.Custom($"Beneficiary {beneficiary.Name} edited successfully", 3, "orange", "fas fa-user-tie");
//                return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = beneficiary.ClaimsInvestigationId });
//            }
//            catch (Exception ex)
//            {
//                Log and notify error
//                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
//                notifyService.Error("OOPS !!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }
//        }

//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(ClaimTransactionModel model)
//        {
//            try
//            {
//                var currentUserEmail = HttpContext.User?.Identity?.Name;

//                if (model is null)
//                {
//                    notifyService.Error("Not Found!!!..Contact Admin");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }
//                var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.ClaimsInvestigation.ClaimsInvestigationId);
//                if (claimsInvestigation == null)
//                {
//                    notifyService.Error("Not Found!!!..Contact Admin");
//                    return RedirectToAction(nameof(Index), "Dashboard");
//                }

//                claimsInvestigation.Updated = DateTime.Now;
//                claimsInvestigation.UpdatedBy = currentUserEmail;
//                claimsInvestigation.Deleted = true;
//                _context.ClaimsInvestigation.Update(claimsInvestigation);
//                await _context.SaveChangesAsync();
//                notifyService.Custom("Claim deleted", 3, "red", "far fa-file-powerpoint");
//                return RedirectToAction(nameof(CreatorManualController.New), "CreatorManual");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                notifyService.Error("OOPS!!!..Contact Admin");
//                return RedirectToAction(nameof(Index), "Dashboard");
//            }

//        }
//    }
//}
