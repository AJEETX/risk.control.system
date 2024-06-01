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
    [Authorize(Roles = CREATOR.DISPLAY_NAME)]
    public class CreatorAutoPostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly ICreatorService creatorService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public CreatorAutoPostController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IClaimsInvestigationService claimsInvestigationService,
            ICreatorService creatorService,
            IFtpService ftpService,
            INotyfService notifyService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.creatorService = creatorService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.investigationReportService = investigationReportService;
        }
        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(IFormFile postedFile, string uploadtype, string uploadingway)
        {
            try
            {
                object _;
                if (!Enum.TryParse(typeof(UploadType), uploadtype, true, out _))
                {
                    notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");
                    return RedirectToAction("New", "CreatorAuto");
                }
                if (postedFile == null || string.IsNullOrWhiteSpace(uploadtype) ||
                string.IsNullOrWhiteSpace(Path.GetFileName(postedFile.FileName)) ||
                string.IsNullOrWhiteSpace(Path.GetExtension(Path.GetFileName(postedFile.FileName))) ||
                Path.GetExtension(Path.GetFileName(postedFile.FileName)) != ".zip"
                )
                {
                    notifyService.Custom($"Upload Error. Contact Admin", 3, "red", "far fa-file-powerpoint");

                    return RedirectToAction("New", "CreatorAuto");
                }
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (postedFile != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    UploadType uploadType = (UploadType)Enum.Parse(typeof(UploadType), uploadtype, true);

                    if (uploadType == UploadType.FTP)
                    {
                        var processed = await ftpService.DownloadFtpFile(userEmail, postedFile, uploadingway);
                        if (processed)
                        {
                            notifyService.Custom($"FTP download complete ", 3, "green", "fa fa-upload");
                        }
                        else
                        {
                            notifyService.Information($"FTP Upload Error. Check limit <i class='fa fa-upload' ></i>", 3);
                        }
                    }

                    if (uploadType == UploadType.FILE && Path.GetExtension(postedFile.FileName) == ".zip")
                    {

                        var processed = await ftpService.UploadFile(userEmail, postedFile, uploadingway);
                        if (processed)
                        {
                            notifyService.Custom($"File upload complete", 3, "green", "fa fa-upload");
                        }
                        else
                        {
                            notifyService.Custom($"File Upload Error.", 3, "red", "fa fa-upload");
                        }

                    }
                }
                return RedirectToAction("New", "CreatorAuto");
            }
            catch (Exception ex)
            {
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [RequestSizeLimit(2_000_000)] // Checking for 2 MB
        [ValidateAntiForgeryToken]
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        public async Task<IActionResult> CreatePolicy(ClaimsInvestigation claimsInvestigation)
        {
            try
            {
                if (claimsInvestigation == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                claimsInvestigation.ClientCompanyId = companyUser?.ClientCompanyId;
                claimsInvestigation.CREATEDBY = CREATEDBY.AUTO;
                claimsInvestigation.ORIGIN = ORIGIN.USER;

                IFormFile documentFile = null;
                IFormFile profileFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.PolicyDetail?.Document?.FileName && f.Name == claimsInvestigation.PolicyDetail?.Document?.Name);
                    if (file != null && file.Length > 2000000)
                    {
                        notifyService.Warning("Uploaded File size morer than 2MB !!! ");
                        return RedirectToAction(nameof(CreatorAutoController.Create), "CreatorAuto", new { claimsInvestigation });
                    }
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
                claimsInvestigation.CREATEDBY = CREATEDBY.AUTO;
                var claim = await claimsInvestigationService.CreatePolicy(userEmail, claimsInvestigation, documentFile, profileFile);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} created successfully", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });

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
        public async Task<IActionResult> EditPolicy(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, string claimtype)
        {
            try
            {
                if (claimsInvestigation == null || string.IsNullOrWhiteSpace(claimsInvestigationId) || claimtype == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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

                notifyService.Custom($"Policy #{claim.PolicyDetail.ContractNumber} edited successfully", 3, "orange", "far fa-file-powerpoint");
                if (string.IsNullOrWhiteSpace(claimtype) || claimtype.Equals("draft", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(CreatorAutoController.Details), "ClaimsInvestigation", new { id = claim.ClaimsInvestigationId });
                }

                else if (claimtype.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });

                }
                else if (claimtype.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
                }
                return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });

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
        public async Task<IActionResult> CreateCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, bool create = true)
        {
            try
            {
                if (claimsInvestigation == null || string.IsNullOrWhiteSpace(claimsInvestigationId))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                IFormFile documentFile = null;
                IFormFile profileFile = null;
                var files = Request.Form?.Files;

                if (files != null && files.Count > 0)
                {
                    //}
                    var file = files.FirstOrDefault(f => f.FileName == claimsInvestigation.CustomerDetail?.ProfileImage?.FileName && f.Name == claimsInvestigation.CustomerDetail?.ProfileImage?.Name);
                    if (file != null && file.Length > 2000000)
                    {
                        notifyService.Warning("Uploaded File size morer than 2MB !!! ");
                        return RedirectToAction(nameof(CreatorAutoController.CreatePolicy), "CreatorAuto", new { claimsInvestigation });
                    }
                    if (file != null)
                    {
                        profileFile = file;
                    }
                }

                var claim = await claimsInvestigationService.CreateCustomer(userEmail, claimsInvestigation, documentFile, profileFile, create);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                notifyService.Custom($"Customer {claim.CustomerDetail.CustomerName} added successfully", 3, "green", "fas fa-user-plus");

                return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });
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
        [Authorize(Roles = CREATOR.DISPLAY_NAME)]
        [HttpPost]
        public async Task<IActionResult> EditCustomer(string claimsInvestigationId, ClaimsInvestigation claimsInvestigation, string claimtype, bool create = true)
        {
            try
            {
                if (claimsInvestigation == null || string.IsNullOrWhiteSpace(claimsInvestigationId) || claimtype == null || string.IsNullOrWhiteSpace(claimtype))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var userEmail = HttpContext.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

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
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                notifyService.Custom($"Customer {claim.CustomerDetail.CustomerName} edited successfully", 3, "orange", "fas fa-user-plus");
                if (claimtype.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = claim.ClaimsInvestigationId });

                }
                else if (claimtype.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });

                }
                return RedirectToAction(nameof(CreatorManualController.Details), "CreatorManual", new { id = claim.ClaimsInvestigationId });
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
        public async Task<IActionResult> CreateBeneficiary(string claimId, BeneficiaryDetail caseLocation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(claimId) || caseLocation is null)
                {
                    notifyService.Error("NOT FOUND  !!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                caseLocation.Updated = DateTime.Now;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;

                IFormFile? customerDocument = Request.Form?.Files?.FirstOrDefault();
                if (customerDocument != null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    caseLocation.ProfilePicture = dataStream.ToArray();
                }

                caseLocation.ClaimsInvestigationId = claimId;
                var pincode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == caseLocation.PinCodeId);

                caseLocation.PinCode = pincode;

                var customerLatLong = caseLocation.PinCode.Latitude + "," + caseLocation.PinCode.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                caseLocation.BeneficiaryLocationMap = url;
                _context.Add(caseLocation);
                await _context.SaveChangesAsync();

                var claimsInvestigation = await _context.ClaimsInvestigation
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == claimId);
                claimsInvestigation.IsReady2Assign = true;

                _context.ClaimsInvestigation.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Beneficiary {caseLocation.BeneficiaryName} added successfully", 3, "green", "fas fa-user-tie");

                return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = caseLocation.ClaimsInvestigationId });

                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", caseLocation.CountryId);
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name", caseLocation.BeneficiaryRelationId);
                ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", caseLocation.DistrictId);
                ViewData["StateId"] = new SelectList(_context.State, "StateId", "StateId", caseLocation.StateId);
                ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Code", caseLocation.PinCodeId);

                return View(caseLocation);
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
        public async Task<IActionResult> EditBeneficiary(long id, BeneficiaryDetail ecaseLocation, string claimtype, long beneficiaryDetailId)
        {
            try
            {
                if (id != ecaseLocation.BeneficiaryDetailId && beneficiaryDetailId != ecaseLocation.BeneficiaryDetailId)
                {
                    notifyService.Error("NOT FOUND!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (id == 0)
                {
                    id = beneficiaryDetailId;
                }
                var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                   i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
                var caseLocation = _context.BeneficiaryDetail.FirstOrDefault(c => c.BeneficiaryDetailId == ecaseLocation.BeneficiaryDetailId);
                caseLocation.Updated = DateTime.Now;
                caseLocation.UpdatedBy = HttpContext.User?.Identity?.Name;
                caseLocation.Addressline = ecaseLocation.Addressline;
                caseLocation.BeneficiaryContactNumber = ecaseLocation.BeneficiaryContactNumber;
                caseLocation.BeneficiaryDateOfBirth = ecaseLocation.BeneficiaryDateOfBirth;
                caseLocation.BeneficiaryIncome = ecaseLocation.BeneficiaryIncome;
                caseLocation.BeneficiaryName = ecaseLocation.BeneficiaryName;
                caseLocation.BeneficiaryRelation = ecaseLocation.BeneficiaryRelation;
                caseLocation.BeneficiaryRelationId = ecaseLocation.BeneficiaryRelationId;
                caseLocation.ClaimsInvestigationId = ecaseLocation.ClaimsInvestigationId;
                caseLocation.CountryId = ecaseLocation.CountryId;
                caseLocation.DistrictId = ecaseLocation.DistrictId;
                caseLocation.PinCodeId = ecaseLocation.PinCodeId;
                caseLocation.StateId = ecaseLocation.StateId;
                var pincode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == caseLocation.PinCodeId);
                caseLocation.PinCode = pincode;
                var customerLatLong = pincode.Latitude + "," + pincode.Longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                caseLocation.BeneficiaryLocationMap = url;

                IFormFile? customerDocument = Request.Form?.Files?.FirstOrDefault();
                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    caseLocation.ProfilePicture = dataStream.ToArray();
                }
                else
                {
                    var existingLocation = _context.BeneficiaryDetail.AsNoTracking().Where(c =>
                        c.BeneficiaryDetailId == caseLocation.BeneficiaryDetailId && c.BeneficiaryDetailId == id).FirstOrDefault();
                    if (existingLocation.ProfilePicture != null || !string.IsNullOrWhiteSpace(existingLocation.ProfilePictureUrl))
                    {
                        caseLocation.ProfilePicture = existingLocation.ProfilePicture;
                        caseLocation.ProfilePictureUrl = existingLocation.ProfilePictureUrl;
                    }
                }

                var pinCode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == caseLocation.PinCodeId);
                caseLocation.PinCode.Latitude = pinCode.Latitude;
                caseLocation.PinCode.Longitude = pinCode.Longitude;

                _context.Update(caseLocation);
                await _context.SaveChangesAsync();
                notifyService.Custom($"Beneficiary {caseLocation.BeneficiaryName} edited successfully", 3, "orange", "fas fa-user-tie");
                return RedirectToAction(nameof(CreatorAutoController.Details), "CreatorAuto", new { id = caseLocation.ClaimsInvestigationId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", ecaseLocation.DistrictId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", ecaseLocation.BeneficiaryRelationId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", ecaseLocation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", ecaseLocation.StateId);
            return View(ecaseLocation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAutoConfirmed(ClaimTransactionModel model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("Not Found!!!..Contact Admin");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
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
                return RedirectToAction(nameof(CreatorAutoController.New), "CreatorAuto");
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
