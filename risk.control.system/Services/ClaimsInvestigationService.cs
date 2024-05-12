using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.Data;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        Task<ClaimsInvestigation> CreatePolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument, bool create = true);

        Task<ClaimsInvestigation> EdiPolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument);

        Task<ClaimsInvestigation> CreateCustomer(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument, bool create = true);

        Task<ClaimsInvestigation> EditCustomer(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? customerDocument);

        Task AssignToAssigner(string userEmail, List<string> claimsInvestigations);

        Task<ClaimsInvestigation> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, long caseLocationId, bool AutoAllocated = true);

        Task<ClaimsInvestigation> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, string claimsInvestigationId);

        Task<ClaimsInvestigation> SubmitToVendorSupervisor(string userEmail, long caseLocationId, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4);

        Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, SupervisorRemarkType remarks);

        Task<ClaimsInvestigation> ProcessCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType);

        List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors);

        Task WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId);

        Task<List<string>> ProcessAutoAllocation(List<string> claims, ClientCompany company, string userEmail);
        Task WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ClaimsInvestigationService(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager,
            IMailboxService mailboxService, IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager)
        {
            this._context = context;
            this.roleManager = roleManager;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<List<string>> ProcessAutoAllocation(List<string> claims, ClientCompany company, string userEmail)
        {
            var autoAllocatedClaims = new List<string>();
            foreach (var claim in claims)
            {
                string pinCode2Verify = string.Empty;
                //1. GET THE PINCODE FOR EACH CLAIM
                var claimsInvestigation = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .First(c => c.ClaimsInvestigationId == claim);
                var beneficiary = _context.BeneficiaryDetail.Include(b => b.PinCode).FirstOrDefault(b => b.ClaimsInvestigationId == claim);

                if (claimsInvestigation.PolicyDetail?.ClaimType == ClaimType.HEALTH)
                {
                    pinCode2Verify = claimsInvestigation.CustomerDetail?.PinCode?.Code;
                }
                else
                {
                    pinCode2Verify = beneficiary.PinCode?.Code;
                }

                var vendorsInPincode = new List<Vendor>();

                //2. GET THE VENDORID FOR EACH CLAIM BASED ON PINCODE
                foreach (var empanelledVendor in company.EmpanelledVendors)
                {
                    foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                    {
                        if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                        {
                            foreach (var pincodeService in serviceType.PincodeServices)
                            {
                                if (pincodeService.Pincode == pinCode2Verify)
                                {
                                    vendorsInPincode.Add(empanelledVendor);
                                    continue;
                                }
                            }
                        }
                        var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                        if (added)
                        {
                            continue;
                        }
                    }
                }

                if (vendorsInPincode.Count == 0)
                {
                    foreach (var empanelledVendor in company.EmpanelledVendors)
                    {
                        foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                        {
                            if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                    serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                            {
                                foreach (var pincodeService in serviceType.PincodeServices)
                                {
                                    if (pincodeService.Pincode.Contains(pinCode2Verify.Substring(0, pinCode2Verify.Length - 2)))
                                    {
                                        vendorsInPincode.Add(empanelledVendor);
                                        continue;
                                    }
                                }
                            }
                            var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                            if (added)
                            {
                                continue;
                            }
                        }
                    }
                }

                if (vendorsInPincode.Count == 0)
                {
                    foreach (var empanelledVendor in company.EmpanelledVendors)
                    {
                        foreach (var serviceType in empanelledVendor.VendorInvestigationServiceTypes)
                        {
                            if (serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                                    serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId)
                            {
                                var pincode = _context.PinCode.Include(p => p.District).FirstOrDefault(p => p.Code == pinCode2Verify);
                                if (serviceType.District.DistrictId == pincode.District.DistrictId)
                                {
                                    vendorsInPincode.Add(empanelledVendor);
                                    continue;
                                }
                            }
                            var added = vendorsInPincode.Any(v => v.VendorId == empanelledVendor.VendorId);
                            if (added)
                            {
                                continue;
                            }
                        }
                    }
                }

                var distinctVendors = vendorsInPincode.Distinct()?.ToList();

                //3. CALL SERVICE WITH VENDORID
                if (vendorsInPincode is not null && vendorsInPincode.Count > 0)
                {
                    var vendorsWithCaseLoad = GetAgencyLoad(distinctVendors).OrderBy(o => o.CaseCount)?.ToList();

                    if (vendorsWithCaseLoad is not null && vendorsWithCaseLoad.Count > 0)
                    {
                        var selectedVendor = vendorsWithCaseLoad.FirstOrDefault();

                        var policy = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId, beneficiary.BeneficiaryDetailId);

                        autoAllocatedClaims.Add(claim);

                        await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId, beneficiary.BeneficiaryDetailId);
                    }
                }
            }
            return autoAllocatedClaims;
        }

        public List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claimsCases = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryDetail)
                .Include(c => c.Vendors)
                .Where(c =>
                c.VendorId.HasValue &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                );

            var vendorCaseCount = new Dictionary<long, int>();

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.BeneficiaryDetail.BeneficiaryDetailId > 0)
                {
                    if (claimsCase.VendorId.HasValue)
                    {
                        if (claimsCase.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                claimsCase.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                )
                        {
                            if (!vendorCaseCount.TryGetValue(claimsCase.VendorId.Value, out countOfCases))
                            {
                                vendorCaseCount.Add(claimsCase.VendorId.Value, 1);
                            }
                            else
                            {
                                int currentCount = vendorCaseCount[claimsCase.VendorId.Value];
                                ++currentCount;
                                vendorCaseCount[claimsCase.VendorId.Value] = currentCount;
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
            return vendorWithCaseCounts;
        }

        public async Task<ClaimsInvestigation> CreatePolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument, bool create = true)
        {
            try
            {
                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    claimsInvestigation.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UserEmailActioned = userEmail;
                claimsInvestigation.UserEmailActionedTo = userEmail;
                claimsInvestigation.UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})";
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.CurrentUserEmail = userEmail;
                claimsInvestigation.CurrentClaimOwner = currentUser.Email;
                claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;

                var aaddedClaimId = _context.ClaimsInvestigation.Add(claimsInvestigation);
                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                    UserEmailActioned = userEmail,
                    UserEmailActionedTo = userEmail,
                    UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})",
                    CurrentClaimOwner = currentUser.Email,
                    HopCount = 0,
                    Time2Update = 0,
                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail
                };
                _context.InvestigationTransaction.Add(log);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigation> EdiPolicy(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument)
        {
            try
            {
                var existingPolicy = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId);
                existingPolicy.PolicyDetail.ContractIssueDate = claimsInvestigation.PolicyDetail.ContractIssueDate;
                existingPolicy.PolicyDetail.InvestigationServiceTypeId = claimsInvestigation.PolicyDetail.InvestigationServiceTypeId;
                existingPolicy.PolicyDetail.ClaimType = claimsInvestigation.PolicyDetail.ClaimType;
                existingPolicy.PolicyDetail.CostCentreId = claimsInvestigation.PolicyDetail.CostCentreId;
                existingPolicy.PolicyDetail.CaseEnablerId = claimsInvestigation.PolicyDetail.CaseEnablerId;
                existingPolicy.PolicyDetail.DateOfIncident = claimsInvestigation.PolicyDetail.DateOfIncident;
                existingPolicy.PolicyDetail.ContractNumber = claimsInvestigation.PolicyDetail.ContractNumber;
                existingPolicy.PolicyDetail.SumAssuredValue = claimsInvestigation.PolicyDetail.SumAssuredValue;
                existingPolicy.PolicyDetail.CauseOfLoss = claimsInvestigation.PolicyDetail.CauseOfLoss;
                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.CurrentUserEmail = userEmail;
                existingPolicy.CurrentClaimOwner = userEmail;

                if (claimDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    claimDocument.CopyTo(dataStream);
                    existingPolicy.PolicyDetail.DocumentImage = dataStream.ToArray();
                }

                _context.ClaimsInvestigation.Update(existingPolicy);

                await _context.SaveChangesAsync();

                return existingPolicy;
            }
            catch (Exception ex)
            {
                throw;
            }
            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigation> EditCustomer(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? customerDocument)
        {
            try
            {
                var existingPolicy = await _context.ClaimsInvestigation
                    .Include(c => c.CustomerDetail)
                        .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId);
                existingPolicy.CustomerDetail.DistrictId = claimsInvestigation.CustomerDetail.DistrictId;
                existingPolicy.CustomerDetail.Addressline = claimsInvestigation.CustomerDetail.Addressline;
                existingPolicy.CustomerDetail.Description = claimsInvestigation.CustomerDetail.Description;
                existingPolicy.CustomerDetail.ContactNumber = claimsInvestigation.CustomerDetail.ContactNumber;
                existingPolicy.CustomerDetail.CountryId = claimsInvestigation.CustomerDetail.CountryId;
                existingPolicy.CustomerDetail.CustomerDateOfBirth = claimsInvestigation.CustomerDetail.CustomerDateOfBirth;
                existingPolicy.CustomerDetail.CustomerEducation = claimsInvestigation.CustomerDetail.CustomerEducation;
                existingPolicy.CustomerDetail.CustomerIncome = claimsInvestigation.CustomerDetail.CustomerIncome;
                existingPolicy.CustomerDetail.CustomerName = claimsInvestigation.CustomerDetail.CustomerName;
                existingPolicy.CustomerDetail.CustomerOccupation = claimsInvestigation.CustomerDetail.CustomerOccupation;
                existingPolicy.CustomerDetail.CustomerType = claimsInvestigation.CustomerDetail.CustomerType;
                existingPolicy.CustomerDetail.Gender = claimsInvestigation.CustomerDetail.Gender;
                existingPolicy.CustomerDetail.PinCodeId = claimsInvestigation.CustomerDetail.PinCodeId;
                existingPolicy.CustomerDetail.StateId = claimsInvestigation.CustomerDetail.StateId;

                existingPolicy.Updated = DateTime.Now;
                existingPolicy.UpdatedBy = userEmail;
                existingPolicy.CurrentUserEmail = userEmail;
                existingPolicy.CurrentClaimOwner = userEmail;

                var pincode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == claimsInvestigation.CustomerDetail.PinCodeId);
                claimsInvestigation.CustomerDetail.PinCode = pincode;
                var customerLatLong = pincode.Latitude + "," + pincode.Longitude;

                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                existingPolicy.CustomerDetail.CustomerLocationMap = url;

                if (customerDocument is not null)
                {
                    using var dataStream = new MemoryStream();
                    customerDocument.CopyTo(dataStream);
                    existingPolicy.CustomerDetail.ProfilePicture = dataStream.ToArray();
                }

                _context.ClaimsInvestigation.Update(existingPolicy);

                await _context.SaveChangesAsync();

                return existingPolicy;
            }
            catch (Exception ex)
            {
                throw;
            }
            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigation> CreateCustomer(string userEmail, ClaimsInvestigation claimsInvestigation, IFormFile? claimDocument, IFormFile? customerDocument, bool create = true)
        {
            if (claimsInvestigation is not null)
            {
                try
                {
                    var existingPolicy = await _context.ClaimsInvestigation
                        .Include(c => c.CustomerDetail)
                        .ThenInclude(c => c.PinCode)
                        .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId);
                    if (customerDocument is not null)
                    {
                        using var dataStream = new MemoryStream();
                        customerDocument.CopyTo(dataStream);

                        claimsInvestigation.CustomerDetail.ProfilePicture = dataStream.ToArray();
                    }
                    var pincode = _context.PinCode.FirstOrDefault(p => p.PinCodeId == claimsInvestigation.CustomerDetail.PinCodeId);

                    claimsInvestigation.CustomerDetail.PinCode = pincode;

                    var customerLatLong = claimsInvestigation.CustomerDetail.PinCode.Latitude + "," + claimsInvestigation.CustomerDetail.PinCode.Longitude;

                    var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Applicationsettings.GMAPData}";
                    claimsInvestigation.CustomerDetail.CustomerLocationMap = url;

                    var addedClaim = _context.CustomerDetail.Add(claimsInvestigation.CustomerDetail);
                    existingPolicy.CustomerDetail = addedClaim.Entity;

                    _context.ClaimsInvestigation.Update(existingPolicy);

                    await _context.SaveChangesAsync();
                    return existingPolicy;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return claimsInvestigation;
        }

        public async Task AssignToAssigner(string userEmail, List<string> claims)
        {
            if (claims is not null && claims.Count > 0)
            {
                var cases2Assign = _context.ClaimsInvestigation
                    .Include(c => c.BeneficiaryDetail)
                    .Where(v => claims.Contains(v.ClaimsInvestigationId));

                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == currentUser.ClientCompanyId);
                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.CREATOR.ToString()));
                var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
                var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
                foreach (var claimsInvestigation in cases2Assign)
                {
                    claimsInvestigation.Updated = DateTime.Now;
                    claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
                    claimsInvestigation.CurrentUserEmail = userEmail;
                    claimsInvestigation.UserEmailActioned = currentUser.Email;
                    claimsInvestigation.UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})";
                    claimsInvestigation.CurrentClaimOwner = currentUser.Email;
                    claimsInvestigation.AssignedToAgency = false;
                    claimsInvestigation.IsReady2Assign = true;
                    claimsInvestigation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;
                    

                    var lastLog = _context.InvestigationTransaction
                        .Where(i =>
                            i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                            .OrderByDescending(o => o.Created)?.FirstOrDefault();

                    var lastLogHop = _context.InvestigationTransaction
                        .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                        .AsNoTracking().Max(s => s.HopCount);

                    var log = new InvestigationTransaction
                    {
                        HopCount = lastLogHop + 1,
                        UserEmailActioned = userEmail,
                        UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})",
                        Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                        CurrentClaimOwner = currentUser.Email,
                        ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                        InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId,
                        UpdatedBy = currentUser.Email
                    };
                    _context.InvestigationTransaction.Add(log);
                }
                _context.UpdateRange(cases2Assign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryDetail)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.PolicyDetail.ClientCompanyId);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            
            claimsInvestigation.Updated = DateTime.Now;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.AssignedToAgency = false;
            claimsInvestigation.CurrentClaimOwner = userEmail;
            claimsInvestigation.UserEmailActioned = userEmail;
            claimsInvestigation.UserEmailActionedTo = userEmail;
            claimsInvestigation.UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({company.Email})";
            claimsInvestigation.CompanyWithdrawlComment = model.ClaimsInvestigation.CompanyWithdrawlComment;
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.ReAssignUploadView = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.VendorId = null;
            claimsInvestigation.Vendor = null;
            //var currentVendor = claimsInvestigation.Vendors.FirstOrDefault(v => v.VendorId == claimsInvestigation.VendorId);
            //currentVendor.SelectedByCompany = false;
            claimsInvestigation.InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseSubStatusId = withdrawnByCompany.InvestigationCaseSubStatusId;
            

            var lastLog = _context.InvestigationTransaction
                .Where(i =>
                    i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                    .OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserEmailActionedTo = userEmail,
                UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({company.Email})",
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                CurrentClaimOwner = userEmail,
                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = withdrawnByCompany.InvestigationCaseSubStatusId,
                UpdatedBy = currentUser.Email
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            try
            {
                _context.SaveChanges();

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryDetail)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.PolicyDetail.ClientCompanyId);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            
            claimsInvestigation.Updated = DateTime.Now;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.AssignedToAgency = false;
            claimsInvestigation.CurrentClaimOwner = currentUser.Email;
            claimsInvestigation.UserEmailActioned = userEmail;
            claimsInvestigation.UserEmailActionedTo = string.Empty;
            claimsInvestigation.AgencyDeclineComment = model.ClaimsInvestigation.AgencyDeclineComment;
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.AssignAutoUploadView = 0;
            claimsInvestigation.VendorId = null;
            claimsInvestigation.UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({company.Email})";
            claimsInvestigation.InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId;

            var lastLog = _context.InvestigationTransaction
                .Where(i =>
                    i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                    .OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                .Where(i => i.ClaimsInvestigationId == claimsInvestigation.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserEmailActionedTo = string.Empty,
                UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ({company.Email})",
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                CurrentClaimOwner = currentUser.Email,
                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId,
                UpdatedBy = currentUser.Email
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            _context.SaveChanges();
        }

        public async Task<ClaimsInvestigation> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, long caseLocationId, bool AutoAllocated = true)
        {
            var vendor = _context.Vendor.FirstOrDefault(v => v.VendorId == vendorId);
            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var supervisor = await GetSupervisor(vendorId);
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var allocatedToVendor = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            if (vendor != null)
            {
                var beneficiaryDetail = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.State)
                .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

                _context.BeneficiaryDetail.Update(beneficiaryDetail);

                var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseToAllocateToVendor.AssignedToAgency = true;
                claimsCaseToAllocateToVendor.Updated = DateTime.Now;
                claimsCaseToAllocateToVendor.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + " (" + currentUser.Email + ")";
                claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
                claimsCaseToAllocateToVendor.CurrentClaimOwner = supervisor.Email;
              
                claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = allocatedToVendor.InvestigationCaseSubStatusId;
                claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
                claimsCaseToAllocateToVendor.UserEmailActionedTo = string.Empty;
                claimsCaseToAllocateToVendor.UserRoleActionedTo =$" {AppRoles.SUPERVISOR.GetEnumDisplayName()} ({vendor.Email})";
                claimsCaseToAllocateToVendor.Vendors.Add(vendor);
                claimsCaseToAllocateToVendor.Vendor = vendor;
                claimsCaseToAllocateToVendor.VendorId = vendorId;
                claimsCaseToAllocateToVendor.AllocateView = 0;
                claimsCaseToAllocateToVendor.AutoAllocated = AutoAllocated;
                _context.ClaimsInvestigation.Update(claimsCaseToAllocateToVendor);
                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsCaseToAllocateToVendor.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var lastLogHop = _context.InvestigationTransaction
                        .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                        .AsNoTracking().Max(s => s.HopCount);

                var log = new InvestigationTransaction
                {
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $" {AppRoles.SUPERVISOR.GetEnumDisplayName()} ({vendor.Email})",
                    UserEmailActionedTo = "",
                    HopCount = lastLogHop + 1,
                    ClaimsInvestigationId = claimsCaseToAllocateToVendor.ClaimsInvestigationId,
                    CurrentClaimOwner = claimsCaseToAllocateToVendor.CurrentClaimOwner,
                    Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = allocatedToVendor.InvestigationCaseSubStatusId,
                    UpdatedBy = currentUser.Email
                };
                _context.InvestigationTransaction.Add(log);

                await _context.SaveChangesAsync();

                return claimsCaseToAllocateToVendor;
            }
            return null;
        }

        public async Task<ClaimsInvestigation> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, string claimsInvestigationId)
        {
            var supervisor = await GetSupervisor(vendorId);
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var assignedToAgent = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claim = _context.ClaimsInvestigation
                .Where(c => c.ClaimsInvestigationId == claimsInvestigationId).FirstOrDefault();
            if (claim != null)
            {
                var agentUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(u => u.Email == vendorAgentEmail);
                claim.UserEmailActioned = currentUser;
                claim.UserEmailActionedTo = agentUser.Email;
                claim.UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agentUser.Vendor.Email})";
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = supervisor.Email;
                claim.CurrentUserEmail = currentUser;
                claim.InvestigateView = 0;
                claim.CurrentClaimOwner = agentUser.Email;
                claim.InvestigationCaseSubStatusId = assignedToAgent.InvestigationCaseSubStatusId;

                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var lastLogHop = _context.InvestigationTransaction
                                        .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                        .AsNoTracking().Max(s => s.HopCount);

                var log = new InvestigationTransaction
                {
                    UserEmailActioned = currentUser,
                    UserEmailActionedTo = agentUser.Email,
                    UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agentUser.Vendor.Email})",
                    HopCount = lastLogHop + 1,
                    ClaimsInvestigationId = claim.ClaimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = assignedToAgent.InvestigationCaseSubStatusId,
                    UpdatedBy = supervisor.FirstName + " " + supervisor.LastName + " (" + supervisor.Email + ")"
                };
                _context.InvestigationTransaction.Add(log);
            }
            _context.ClaimsInvestigation.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<ClaimsInvestigation> SubmitToVendorSupervisor(string userEmail, long caseLocationId, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4)
        {
            var agent = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var submitted2Supervisor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var supervisor = await GetSupervisor(agent.VendorId.Value);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            claim.VerifyView = 0;
            claim.InvestigateView = 0;
            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{AppRoles.SUPERVISOR.GetEnumDisplayName()} ({agent.Vendor.Email})";
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = agent.FirstName + " " + agent.LastName + "(" + agent.Email + ")";
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = supervisor.Email;
            claim.InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId;

            var caseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var claimReport = _context.ClaimReport.Include(c => c.ReportQuestionaire).FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);

            claimReport.ReportQuestionaire.Answer1 = answer1;

            if (answer2 == "0" || answer2 == "0.0")
            {
                answer2 = "Low";
            }
            else if (answer2 == ".5" || answer2 == "0.5")
            {
                answer2 = "Medium";
            }
            else if (answer2 == "1" || answer2 == "1.0")
            {
                answer2 = "High";
            }

            claimReport.ReportQuestionaire.Answer2 = answer2;
            claimReport.ReportQuestionaire.Answer3 = answer3;
            claimReport.ReportQuestionaire.Answer4 = answer4;
            claimReport.AgentRemarks = remarks;
            claimReport.AgentRemarksUpdated = DateTime.Now;
            claimReport.AgentEmail = userEmail;
            _context.ClaimReport.Update(claimReport);

            caseLocation.Updated = DateTime.Now;
            caseLocation.UpdatedBy = userEmail;
            _context.BeneficiaryDetail.Update(caseLocation);

            var lastLog = _context.InvestigationTransaction.Where(i =>
               i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                       .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = agent.Email,
                UserRoleActionedTo = $"{AppRoles.SUPERVISOR.GetEnumDisplayName()} ({agent.Vendor.Email})",
                HopCount = lastLogHop + 1,
                CurrentClaimOwner = supervisor.Email,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId,
                UpdatedBy = agent.Email
            };
            _context.InvestigationTransaction.Add(log);

            _context.ClaimsInvestigation.Update(claim);

            try
            {
                await _context.SaveChangesAsync();
                return claim;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, SupervisorRemarkType reportUpdateStatus)
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, claimsInvestigationId, caseLocationId, supervisorRemarks, reportUpdateStatus);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAllocateToVendorAgent(userEmail, claimsInvestigationId, caseLocationId, supervisorRemarks, reportUpdateStatus);
            }
        }

        public async Task<ClaimsInvestigation> ProcessCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType reportUpdateStatus)
        {
            var claim = _context.ClaimsInvestigation
                 .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                return await ApproveCaseReport(userEmail, assessorRemarks, caseLocationId, claimsInvestigationId, reportUpdateStatus);
            }
            else if (reportUpdateStatus == AssessorRemarkType.REJECT)
            {
                //PUT th case back in review list :: Assign back to Agent
                return await RejectCaseReport(userEmail, assessorRemarks, caseLocationId, claimsInvestigationId, reportUpdateStatus);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAssignToCreator(userEmail, claimsInvestigationId, caseLocationId, assessorRemarks, reportUpdateStatus);
            }
        }

        private async Task<ClaimsInvestigation> RejectCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType)
        {
            var caseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);
            var rejected = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);
            caseLocation.ClaimReport.AssessorRemarkType = assessorRemarkType;
            caseLocation.ClaimReport.AssessorRemarks = assessorRemarks;
            caseLocation.ClaimReport.AssessorRemarksUpdated = DateTime.Now;
            caseLocation.ClaimReport.AssessorEmail = userEmail;

            caseLocation.Updated = DateTime.Now;
            caseLocation.UpdatedBy = userEmail;
            _context.BeneficiaryDetail.Update(caseLocation);
            try
            {
                await _context.SaveChangesAsync();
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(p => p.ClientCompany)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);
                claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId;
                claim.Updated = DateTime.Now;
                claim.UserEmailActioned = userEmail;
                claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.PolicyDetail.ClientCompany.Email})";
                claim.UserEmailActionedTo = userEmail;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);

                var finalLog = new InvestigationTransaction
                {
                    HopCount = finalHop + 1,
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.PolicyDetail.ClientCompany.Email})",
                    ClaimsInvestigationId = claimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Created = DateTime.Now,
                    Time2Update = DateTime.Now.Subtract(claim.Created).Days,
                    InvestigationCaseStatusId = finished.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail
                };

                _context.InvestigationTransaction.Add(finalLog);

                //create invoice

                var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == claim.VendorId);
                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
                if (investigationServiced == null)
                {
                    investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
                }
                //END
                var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                var invoice = new VendorInvoice
                {
                    ClientCompanyId = currentUser.ClientCompany.ClientCompanyId,
                    GrandTotal = investigationServiced.Price + investigationServiced.Price * 10,
                    NoteToRecipient = "Auto generated Invoice",
                    Updated = DateTime.Now,
                    Vendor = vendor,
                    ClientCompany = currentUser.ClientCompany,
                    UpdatedBy = userEmail,
                    VendorId = vendor.VendorId,
                    ClaimReportId = caseLocation.ClaimReport?.ClaimReportId,
                    SubTotal = investigationServiced.Price,
                    TaxAmount = investigationServiced.Price * 10,
                    InvestigationServiceType = investigatService,
                    ClaimId = claimsInvestigationId
                };

                _context.VendorInvoice.Add(invoice);

                var saveCount = await _context.SaveChangesAsync();

                return saveCount > 0 ? claim : null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null!;
        }
        private async Task<ClaimsInvestigation> ApproveCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType)
        {
            var caseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);
            var approved = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);
            caseLocation.ClaimReport.AssessorRemarkType = assessorRemarkType;
            caseLocation.ClaimReport.AssessorRemarks = assessorRemarks;
            caseLocation.ClaimReport.AssessorRemarksUpdated = DateTime.Now;
            caseLocation.ClaimReport.AssessorEmail = userEmail;

            caseLocation.Updated = DateTime.Now;
            caseLocation.UpdatedBy = userEmail;
            _context.BeneficiaryDetail.Update(caseLocation);
            try
            {
                await _context.SaveChangesAsync();
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(p=>p.ClientCompany)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);
                claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId;
                claim.Updated = DateTime.Now;
                claim.UserEmailActioned = userEmail;
                claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.PolicyDetail.ClientCompany.Email})";
                claim.UserEmailActionedTo = userEmail;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);

                var finalLog = new InvestigationTransaction
                {
                    HopCount = finalHop + 1,
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.PolicyDetail.ClientCompany.Email})",
                    ClaimsInvestigationId = claimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Created = DateTime.Now,
                    Time2Update = DateTime.Now.Subtract(claim.Created).Days,
                    InvestigationCaseStatusId = finished.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId,
                    UpdatedBy = userEmail
                };

                _context.InvestigationTransaction.Add(finalLog);

                //create invoice

                var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == claim.VendorId);
                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
                if (investigationServiced == null)
                {
                    investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
                }
                //END
                var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                var invoice = new VendorInvoice
                {
                    ClientCompanyId = currentUser.ClientCompany.ClientCompanyId,
                    GrandTotal = investigationServiced.Price + investigationServiced.Price * 10,
                    NoteToRecipient = "Auto generated Invoice",
                    Updated = DateTime.Now,
                    Vendor = vendor,
                    ClientCompany = currentUser.ClientCompany,
                    UpdatedBy = userEmail,
                    VendorId = vendor.VendorId,
                    ClaimReportId = caseLocation.ClaimReport?.ClaimReportId,
                    SubTotal = investigationServiced.Price,
                    TaxAmount = investigationServiced.Price * 10,
                    InvestigationServiceType = investigatService,
                    ClaimId = claimsInvestigationId
                };

                _context.VendorInvoice.Add(invoice);

                var saveCount = await _context.SaveChangesAsync();

                return saveCount > 0 ? claim : null!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null!;
        }

        private async Task<VendorApplicationUser> GetSupervisor(long vendorId)
        {
            var vendorNonAdminUsers = _context.VendorApplicationUser.Where(u =>
            u.VendorId == vendorId && !u.IsVendorAdmin);

            var supervisor = roleManager.Roles.FirstOrDefault(r =>
                r.Name.Contains(AppRoles.SUPERVISOR.ToString()));

            foreach (var vendorNonAdminUser in vendorNonAdminUsers)
            {
                if (await userManager.IsInRoleAsync(vendorNonAdminUser, supervisor?.Name))
                {
                    return vendorNonAdminUser;
                }
            }
            return null;
        }

        private async Task<ClaimsInvestigation> ReAssignToCreator(string userEmail, string claimsInvestigationId, long caseLocationId, string assessorRemarks, AssessorRemarkType assessorRemarkType)
        {
            var claimsCaseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.PreviousClaimReports)
                .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            claimsCaseLocation.ClaimReport.AssessorRemarkType = assessorRemarkType;
            claimsCaseLocation.ClaimReport.AssessorRemarks = assessorRemarks;
            claimsCaseLocation.ClaimReport.AssessorRemarksUpdated = DateTime.Now;
            claimsCaseLocation.ClaimReport.AssessorEmail = userEmail;
            var reAssigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var saveReport = new PreviousClaimReport
            {
                ClaimsInvestigationId = claimsInvestigationId,
                AgentEmail = claimsCaseLocation.ClaimReport.AgentEmail,
                DigitalIdReport = claimsCaseLocation.ClaimReport.DigitalIdReport,
                DocumentIdReport = claimsCaseLocation.ClaimReport.DocumentIdReport,
                AgentRemarks = claimsCaseLocation.ClaimReport.AgentRemarks,
                AgentRemarksUpdated = claimsCaseLocation.ClaimReport.AssessorRemarksUpdated,
                AssessorEmail = claimsCaseLocation.ClaimReport.AssessorEmail,
                AssessorRemarks = claimsCaseLocation.ClaimReport.AssessorRemarks,
                AssessorRemarkType = claimsCaseLocation.ClaimReport.AssessorRemarkType,
                AssessorRemarksUpdated = claimsCaseLocation.ClaimReport.AssessorRemarksUpdated,
                ReportQuestionaire = claimsCaseLocation.ClaimReport.ReportQuestionaire,
                SupervisorEmail = claimsCaseLocation.ClaimReport.SupervisorEmail,
                SupervisorRemarks = claimsCaseLocation.ClaimReport.SupervisorRemarks,
                SupervisorRemarksUpdated = claimsCaseLocation.ClaimReport.SupervisorRemarksUpdated,
                SupervisorRemarkType = claimsCaseLocation.ClaimReport.SupervisorRemarkType,
                Updated = claimsCaseLocation.Updated,
                UpdatedBy = claimsCaseLocation.UpdatedBy,
            };
            var currentSavedReport = _context.PreviousClaimReport.Add(saveReport);

            var newReport = new ClaimReport
            {
                ClaimsInvestigationId = claimsInvestigationId,
                ReportQuestionaire = new ReportQuestionaire(),
                DocumentIdReport = new DocumentIdReport(),
                DigitalIdReport = new DigitalIdReport()
            };
            claimsCaseLocation.ClaimReport = newReport;
            _context.ClaimReport.Add(newReport);
            _context.BeneficiaryDetail.Update(claimsCaseLocation);

            var claimsCaseToReassign = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
            claimsCaseToReassign.VendorId = 0;
            claimsCaseToReassign.AssignedToAgency = false;
            claimsCaseToReassign.ReviewCount += 1;
            claimsCaseToReassign.UserEmailActioned = userEmail;
            claimsCaseToReassign.UserEmailActionedTo = string.Empty;
            claimsCaseToReassign.UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ( {currentUser.ClientCompany.Email})";
            claimsCaseToReassign.Updated = DateTime.Now;
            claimsCaseToReassign.UpdatedBy = userEmail;
            claimsCaseToReassign.CurrentUserEmail = userEmail;
            claimsCaseToReassign.IsReviewCase = true;
            claimsCaseToReassign.AssessView = 0;
            claimsCaseToReassign.ActiveView = 0;
            claimsCaseToReassign.AllocateView = 0;
            claimsCaseToReassign.VerifyView = 0;
            claimsCaseToReassign.AssessView = 0;
            claimsCaseToReassign.ReAssignUploadView = 0;
            claimsCaseToReassign.CurrentClaimOwner = currentUser.Email;
            claimsCaseToReassign.InvestigationCaseSubStatusId = reAssigned.InvestigationCaseSubStatusId;

            _context.ClaimsInvestigation.Update(claimsCaseToReassign);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                            i.ClaimsInvestigationId == claimsCaseToReassign.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                IsReviewCase = true,
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{AppRoles.CREATOR.GetEnumDisplayName()} ( {currentUser.ClientCompany.Email})",
                ClaimsInvestigationId = claimsCaseToReassign.ClaimsInvestigationId,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = reAssigned.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                CurrentClaimOwner = currentUser.Email
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? claimsCaseToReassign : null;
        }

        private async Task<ClaimsInvestigation> ApproveAgentReport(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var caseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            var clientCompany = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId);

            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(s => s.Email == userEmail);
            var submitted2Assessor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();
            claim.AssignedToAgency = false;
            claim.IsReviewCase = false;
            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{AppRoles.ASSESSOR.GetEnumDisplayName()} ( {clientCompany.Email})";
            claim.UserEmailActionedTo = string.Empty;
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = agencyUser.Email;
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = agencyUser.Email;
            claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
            claim.InvestigationCaseSubStatusId = submitted2Assessor.InvestigationCaseSubStatusId;

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;
            report.SupervisorRemarksUpdated = DateTime.Now;
            report.SupervisorEmail = userEmail;
            report.Vendor = claim.Vendor;
            _context.ClaimReport.Update(report);
            caseLocation.ClaimReport = report;
            caseLocation.Updated = DateTime.Now;
            caseLocation.UpdatedBy = userEmail;
            _context.BeneficiaryDetail.Update(caseLocation);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{AppRoles.ASSESSOR.GetEnumDisplayName()} ( {clientCompany.Email})",
                CurrentClaimOwner = agencyUser.Email,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = submitted2Assessor.InvestigationCaseSubStatusId,
                UpdatedBy = agencyUser.Email
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claim);
            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<ClaimsInvestigation> ReAllocateToVendorAgent(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var agencyUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(s => s.Email == userEmail);

            var claimsCaseLocation = _context.BeneficiaryDetail
            .Include(c => c.ClaimReport)
            .Include(c => c.ClaimsInvestigation)
            .Include(c => c.PinCode)
            .Include(c => c.District)
            .Include(c => c.State)
            .Include(c => c.State)
            .FirstOrDefault(c => c.BeneficiaryDetailId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == claimsCaseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;

            _context.ClaimReport.Update(report);
            claimsCaseLocation.ClaimReport = report;
            _context.BeneficiaryDetail.Update(claimsCaseLocation);

            var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(p=>p.ClientCompany)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
            claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
            claimsCaseToAllocateToVendor.UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agencyUser.Vendor.Email})";
            claimsCaseToAllocateToVendor.Updated = DateTime.Now;
            claimsCaseToAllocateToVendor.UpdatedBy = userEmail;
            claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
            claimsCaseToAllocateToVendor.IsReviewCase = true;
            claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId;
            
            _context.ClaimsInvestigation.Update(claimsCaseToAllocateToVendor);

            var lastLog = _context.InvestigationTransaction.Where(i =>
                 i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                        .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                 .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserRoleActionedTo= $"{AppRoles.CREATOR.GetEnumDisplayName()} ({claimsCaseToAllocateToVendor.PolicyDetail.ClientCompany.Email})",
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? claimsCaseToAllocateToVendor : null;
        }
    }
}