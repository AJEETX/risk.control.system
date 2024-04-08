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
                var beneficiary = _context.CaseLocation.Include(b => b.PinCode).FirstOrDefault(b => b.ClaimsInvestigationId == claim);

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

                        var policy = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId, beneficiary.CaseLocationId);

                        autoAllocatedClaims.Add(claim);

                        await mailboxService.NotifyClaimAllocationToVendor(userEmail, policy.PolicyDetail.ContractNumber, claimsInvestigation.ClaimsInvestigationId, selectedVendor.Vendor.VendorId, beneficiary.CaseLocationId);
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
                .Include(c => c.Vendors)
                .Include(c => c.CaseLocations.Where(c =>
                c.VendorId.HasValue &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                ));

            var vendorCaseCount = new Dictionary<long, int>();

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.CaseLocations.Count > 0)
                {
                    foreach (var CaseLocation in claimsCase.CaseLocations)
                    {
                        if (CaseLocation.VendorId.HasValue)
                        {
                            if (CaseLocation.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    CaseLocation.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId
                                    )
                            {
                                if (!vendorCaseCount.TryGetValue(CaseLocation.VendorId.Value, out countOfCases))
                                {
                                    vendorCaseCount.Add(CaseLocation.VendorId.Value, 1);
                                }
                                else
                                {
                                    int currentCount = vendorCaseCount[CaseLocation.VendorId.Value];
                                    ++currentCount;
                                    vendorCaseCount[CaseLocation.VendorId.Value] = currentCount;
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

                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UserEmailActioned = userEmail;
                claimsInvestigation.UserEmailActionedTo = userEmail;
                claimsInvestigation.UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})";
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
                    UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})",
                    CurrentClaimOwner = currentUser.Email,
                    Created = DateTime.UtcNow,
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
                existingPolicy.Updated = DateTime.UtcNow;
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

                existingPolicy.Updated = DateTime.UtcNow;
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
                    if (existingPolicy != null)
                    {
                        existingPolicy.Updated = DateTime.UtcNow;
                        existingPolicy.UpdatedBy = userEmail;
                        existingPolicy.CurrentUserEmail = userEmail;
                        existingPolicy.CurrentClaimOwner = userEmail;
                        existingPolicy.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED).InvestigationCaseStatusId;
                        existingPolicy.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR).InvestigationCaseSubStatusId;
                    }

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
                    .Include(c => c.CaseLocations)
                    .Where(v => claims.Contains(v.ClaimsInvestigationId));

                var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == currentUser.ClientCompanyId);
                string currentOwner = string.Empty;
                var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Creator.ToString()));
                //var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));

                foreach (var companyUser in companyUsers)
                {
                    var isCeatorr = await userManager.IsInRoleAsync(companyUser, creatorRole?.Name);
                    if (isCeatorr)
                    {
                        currentOwner = companyUser.Email;
                        break;
                    }
                }
                var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
                var reassigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
                foreach (var claimsInvestigation in cases2Assign)
                {
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
                    claimsInvestigation.CurrentUserEmail = userEmail;
                    claimsInvestigation.UserEmailActioned = userEmail;
                    claimsInvestigation.UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})";
                    claimsInvestigation.CurrentClaimOwner = currentOwner;
                    claimsInvestigation.AssignedToAgency = true;
                    claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
                    claimsInvestigation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;
                    foreach (var caseLocation in claimsInvestigation.CaseLocations)
                    {
                        caseLocation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;
                    }

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
                        UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({currentUser.ClientCompany.Email})",
                        Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                        CurrentClaimOwner = currentOwner,
                        ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                        Created = DateTime.UtcNow,
                        InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId,
                        UpdatedBy = currentUser.Email
                    };
                    _context.InvestigationTransaction.Add(log);
                }
                _context.UpdateRange(cases2Assign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.PolicyDetail.ClientCompanyId);
            var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == claimsInvestigation.PolicyDetail.ClientCompanyId);

            string currentOwner = string.Empty;
            var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Creator.ToString()));
            //var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assigner.ToString()));
            var assigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            foreach (var companyUser in companyUsers)
            {
                var isCeatorr = await userManager.IsInRoleAsync(companyUser, creatorRole?.Name);
                if (isCeatorr)
                {
                    currentOwner = companyUser.Email;
                    break;
                }
            }
            claimsInvestigation.PolicyDetail.Comments = model.ClaimsInvestigation.PolicyDetail.Comments;
            claimsInvestigation.Updated = DateTime.UtcNow;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.CurrentClaimOwner = currentOwner;
            claimsInvestigation.UserEmailActioned = userEmail;
            claimsInvestigation.UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({company.Email})";
            claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
            claimsInvestigation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;
            foreach (var caseLocation in claimsInvestigation.CaseLocations)
            {
                caseLocation.InvestigationCaseSubStatusId = assigned.InvestigationCaseSubStatusId;
                caseLocation.Vendor = null;
            }

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
                UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({company.Email})",
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                CurrentClaimOwner = currentOwner,
                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                Created = DateTime.UtcNow,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId,
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

            if (vendor != null)
            {
                var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.State)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

                claimsCaseLocation.Vendor = vendor;
                claimsCaseLocation.VendorId = vendorId;
                claimsCaseLocation.AssignedAgentUserEmail = supervisor.Email;
                claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
                _context.CaseLocation.Update(claimsCaseLocation);

                var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseToAllocateToVendor.AssignedToAgency = true;
                claimsCaseToAllocateToVendor.Updated = DateTime.UtcNow;
                claimsCaseToAllocateToVendor.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + " (" + currentUser.Email + ")";
                claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
                claimsCaseToAllocateToVendor.CurrentClaimOwner = supervisor.Email;
                claimsCaseToAllocateToVendor.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
                claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
                var existinCaseLocation = claimsCaseToAllocateToVendor.CaseLocations.FirstOrDefault(c => c.CaseLocationId == caseLocationId);
                existinCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
                claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
                claimsCaseToAllocateToVendor.UserEmailActionedTo = string.Empty;
                claimsCaseToAllocateToVendor.UserRoleActionedTo =$" {AppRoles.Supervisor.GetEnumDisplayName()} ({vendor.Email})";
                claimsCaseToAllocateToVendor.Vendors.Add(vendor);
                claimsCaseToAllocateToVendor.VendorId = vendorId;
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
                    UserRoleActionedTo = $" {AppRoles.Supervisor.GetEnumDisplayName()} ({vendor.Email})",
                    UserEmailActionedTo = "",
                    HopCount = lastLogHop + 1,
                    ClaimsInvestigationId = claimsCaseToAllocateToVendor.ClaimsInvestigationId,
                    CurrentClaimOwner = claimsCaseToAllocateToVendor.CurrentClaimOwner,
                    Created = DateTime.UtcNow,
                    Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId,
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

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Where(c => c.ClaimsInvestigationId == claimsInvestigationId).FirstOrDefault();
            if (claim != null)
            {
                var claimsCaseLocation = _context.CaseLocation
                    .Include(c => c.ClaimsInvestigation)
                    .Include(c => c.InvestigationCaseSubStatus)
                    .Include(c => c.Vendor)
                    .Include(c => c.PinCode)
                    .Include(c => c.District)
                    .Include(c => c.State)
                    .Include(c => c.ClaimReport)
                    .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DigitalIdReport)
                    .Include(c => c.ClaimReport)
                    .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DocumentIdReport)
                    .Include(c => c.ClaimReport)
                    .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.ReportQuestionaire)
                    .FirstOrDefault(c => c.VendorId == vendorId && c.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseLocation.AssignedAgentUserEmail = vendorAgentEmail;
                claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId;

                var template = _context.ServiceReportTemplate.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId
                && c.LineOfBusinessId == claim.PolicyDetail.LineOfBusinessId
                && c.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

                claimsCaseLocation.ClaimReport.ServiceReportTemplate = template;

                _context.CaseLocation.Update(claimsCaseLocation);

                var agentUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(u => u.Email == vendorAgentEmail);
                claim.UserEmailActioned = currentUser;
                claim.UserEmailActionedTo = agentUser.Email;
                claim.UserRoleActionedTo = $"{AppRoles.Agent.GetEnumDisplayName()} ({agentUser.Vendor.Email})";
                claim.Updated = DateTime.UtcNow;
                claim.UpdatedBy = supervisor.Email;
                claim.CurrentUserEmail = currentUser;
                claim.CurrentClaimOwner = agentUser.Email;
                claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId;

                var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

                var lastLogHop = _context.InvestigationTransaction
                                        .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                        .AsNoTracking().Max(s => s.HopCount);

                var log = new InvestigationTransaction
                {
                    UserEmailActioned = currentUser,
                    UserEmailActionedTo = agentUser.Email,
                    UserRoleActionedTo = $"{AppRoles.Agent.GetEnumDisplayName()} ({agentUser.Vendor.Email})",
                    HopCount = lastLogHop + 1,
                    ClaimsInvestigationId = claim.ClaimsInvestigationId,
                    CurrentClaimOwner = claim.CurrentClaimOwner,
                    Created = DateTime.UtcNow,
                    Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                    InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).InvestigationCaseSubStatusId,
                    UpdatedBy = supervisor.FirstName + " " + supervisor.LastName + " (" + supervisor.Email + ")"
                };
                _context.InvestigationTransaction.Add(log);
            }
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<ClaimsInvestigation> SubmitToVendorSupervisor(string userEmail, long caseLocationId, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4)
        {
            var agent = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());

            var supervisor = await GetSupervisor(agent.VendorId.Value);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{AppRoles.Supervisor.GetEnumDisplayName()} ({agent.Vendor.Email})";
            claim.Updated = DateTime.UtcNow;
            claim.UpdatedBy = agent.FirstName + " " + agent.LastName + "(" + agent.Email + ")";
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = supervisor.Email;
            claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
            claim.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;

            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

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
            claimReport.AgentRemarksUpdated = DateTime.UtcNow;
            claimReport.AgentEmail = userEmail;
            _context.ClaimReport.Update(claimReport);

            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = supervisor.Email;
            caseLocation.IsReviewCaseLocation = false;
            _context.CaseLocation.Update(caseLocation);

            var lastLog = _context.InvestigationTransaction.Where(i =>
               i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                       .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = agent.Email,
                UserRoleActionedTo = $"{AppRoles.Supervisor.GetEnumDisplayName()} ({agent.Vendor.Email})",
                HopCount = lastLogHop + 1,
                CurrentClaimOwner = supervisor.Email,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId,
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
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAssignToCreator(userEmail, claimsInvestigationId, caseLocationId, assessorRemarks, reportUpdateStatus);
            }
        }

        private async Task<ClaimsInvestigation> ApproveCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType)
        {
            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            caseLocation.ClaimReport.AssessorRemarkType = assessorRemarkType;
            caseLocation.ClaimReport.AssessorRemarks = assessorRemarks;
            caseLocation.ClaimReport.AssessorRemarksUpdated = DateTime.UtcNow;
            caseLocation.ClaimReport.AssessorEmail = userEmail;

            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            _context.CaseLocation.Update(caseLocation);
            try
            {
                await _context.SaveChangesAsync();
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(p=>p.ClientCompany)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                if (claim != null && claim.CaseLocations.All(c => c.InvestigationCaseSubStatusId == _context.InvestigationCaseSubStatus
                    .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId))
                {
                    claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED).InvestigationCaseStatusId;
                    claim.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId;
                    claim.Updated = DateTime.UtcNow;
                    claim.UserEmailActioned = userEmail;
                    claim.UserRoleActionedTo = $"{AppRoles.CompanyAdmin.GetEnumDisplayName()} ({claim.PolicyDetail.ClientCompany.Email})";
                    claim.UserEmailActionedTo = userEmail;
                    _context.ClaimsInvestigation.Update(claim);

                    var finalHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                        .AsNoTracking().Max(s => s.HopCount);

                    var finalLog = new InvestigationTransaction
                    {
                        HopCount = finalHop + 1,
                        UserEmailActioned = userEmail,
                        UserRoleActionedTo = $"{AppRoles.CompanyAdmin.GetEnumDisplayName()} ({claim.PolicyDetail.ClientCompany.Email})",
                        ClaimsInvestigationId = claimsInvestigationId,
                        CurrentClaimOwner = claim.CurrentClaimOwner,
                        Created = DateTime.UtcNow,
                        Time2Update = DateTime.UtcNow.Subtract(claim.Created).Days,
                        InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED).InvestigationCaseStatusId,
                        InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                        i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR).InvestigationCaseSubStatusId,
                        UpdatedBy = userEmail
                    };

                    _context.InvestigationTransaction.Add(finalLog);

                    //create invoice

                    var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == caseLocation.VendorId);
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
                        Updated = DateTime.UtcNow,
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
                r.Name.Contains(AppRoles.Supervisor.ToString()));

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
            var claimsCaseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Vendor)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.PreviousClaimReports)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var companyUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == currentUser.ClientCompanyId);
            string currentOwner = string.Empty;
            var creatorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Creator.ToString()));
            
            foreach (var companyUser in companyUsers)
            {
                var isAssigner = await userManager.IsInRoleAsync(companyUser, creatorRole?.Name);
                if (isAssigner)
                {
                    currentOwner = companyUser.Email;
                    break;
                }
            }

            claimsCaseLocation.ClaimReport.AssessorRemarkType = assessorRemarkType;
            claimsCaseLocation.ClaimReport.AssessorRemarks = assessorRemarks;
            claimsCaseLocation.ClaimReport.AssessorRemarksUpdated = DateTime.UtcNow;
            claimsCaseLocation.ClaimReport.AssessorEmail = userEmail;

            var saveReport = new PreviousClaimReport
            {
                AgentEmail = claimsCaseLocation.ClaimReport.AgentEmail,
                DigitalIdReport = claimsCaseLocation.ClaimReport.DigitalIdReport,
                DocumentIdReport = claimsCaseLocation.ClaimReport.DocumentIdReport,
                AgentRemarks = claimsCaseLocation.ClaimReport.AgentRemarks,
                AgentRemarksUpdated = claimsCaseLocation.ClaimReport.AssessorRemarksUpdated,
                AssessorEmail = claimsCaseLocation.ClaimReport.AssessorEmail,
                AssessorRemarks = claimsCaseLocation.ClaimReport.AssessorRemarks,
                AssessorRemarkType = claimsCaseLocation.ClaimReport.AssessorRemarkType,
                AssessorRemarksUpdated = claimsCaseLocation.ClaimReport.AssessorRemarksUpdated,
                CaseLocation = claimsCaseLocation,
                CaseLocationId = claimsCaseLocation.CaseLocationId,
                ReportQuestionaire = claimsCaseLocation.ClaimReport.ReportQuestionaire,
                SupervisorEmail = claimsCaseLocation.ClaimReport.SupervisorEmail,
                SupervisorRemarks = claimsCaseLocation.ClaimReport.SupervisorRemarks,
                SupervisorRemarksUpdated = claimsCaseLocation.ClaimReport.SupervisorRemarksUpdated,
                SupervisorRemarkType = claimsCaseLocation.ClaimReport.SupervisorRemarkType,
                Vendor = claimsCaseLocation.Vendor,
                VendorId = claimsCaseLocation.VendorId,
                Updated = claimsCaseLocation.Updated,
                UpdatedBy = claimsCaseLocation.UpdatedBy,
            };
            var currentSavedReport = _context.PreviousClaimReport.Add(saveReport);
            claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;
            claimsCaseLocation.IsReviewCaseLocation = true;

            var newReport = new ClaimReport
            {
                CaseLocation = claimsCaseLocation,
                CaseLocationId = claimsCaseLocation.CaseLocationId,
                Vendor = claimsCaseLocation.Vendor,
                ReportQuestionaire = new ReportQuestionaire(),
                DocumentIdReport = new DocumentIdReport(),
                DigitalIdReport = new DigitalIdReport()
            };
            claimsCaseLocation.ClaimReport = newReport;
            _context.ClaimReport.Add(newReport);
            _context.CaseLocation.Update(claimsCaseLocation);

            var claimsCaseToReassign = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
            claimsCaseToReassign.UserEmailActioned = userEmail;
            claimsCaseToReassign.UserEmailActionedTo = string.Empty;
            claimsCaseToReassign.UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ( {currentUser.ClientCompany.Email})";
            claimsCaseToReassign.Updated = DateTime.UtcNow;
            claimsCaseToReassign.UpdatedBy = userEmail;
            claimsCaseToReassign.CurrentUserEmail = userEmail;
            claimsCaseToReassign.IsReviewCase = true;
            claimsCaseToReassign.CurrentClaimOwner = currentOwner;
            claimsCaseToReassign.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId;

            _context.ClaimsInvestigation.Update(claimsCaseToReassign);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                            i.ClaimsInvestigationId == claimsCaseToReassign.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ( {currentUser.ClientCompany.Email})",
                ClaimsInvestigationId = claimsCaseToReassign.ClaimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail,
                CurrentClaimOwner = currentOwner
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? claimsCaseToReassign : null;
        }

        private async Task<ClaimsInvestigation> ApproveAgentReport(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var caseLocation = _context.CaseLocation
                .Include(c => c.ClaimReport)
                .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.Vendor)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            var assessorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Assessor.ToString()));
            var supervisorRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.Supervisor.ToString()));

            var clientCompany = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId);

            var agencyUser = _context.VendorApplicationUser.FirstOrDefault(s => s.Email == userEmail);

            string supervisorUser = string.Empty;

            var isSupervisor = await userManager.IsInRoleAsync(agencyUser, supervisorRole?.Name);
            if (isSupervisor)
            {
                supervisorUser = agencyUser.Email;
            }

            var companyUsers = _context.ClientCompanyApplicationUser.Where(c => c.ClientCompanyId == clientCompany.ClientCompanyId);
            string currentOwner = string.Empty;
            foreach (var companyUser in companyUsers)
            {
                var isAssigner = await userManager.IsInRoleAsync(companyUser, assessorRole?.Name);
                if (isAssigner)
                {
                    currentOwner = companyUser.Email;
                    break;
                }
            }
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{AppRoles.Assessor.GetEnumDisplayName()} ( {clientCompany.Email})";
            claim.UserEmailActionedTo = string.Empty;
            claim.Updated = DateTime.UtcNow;
            claim.UpdatedBy = supervisorUser;
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = currentOwner;
            claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
            claim.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR).InvestigationCaseSubStatusId;

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == caseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;
            report.SupervisorRemarksUpdated = DateTime.UtcNow;
            report.SupervisorEmail = userEmail;
            report.Vendor = claim.Vendor;
            _context.ClaimReport.Update(report);
            caseLocation.ClaimReport = report;
            caseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR).InvestigationCaseSubStatusId;
            caseLocation.Updated = DateTime.UtcNow;
            caseLocation.UpdatedBy = userEmail;
            caseLocation.AssignedAgentUserEmail = string.Empty;
            _context.CaseLocation.Update(caseLocation);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{AppRoles.Assessor.GetEnumDisplayName()} ( {clientCompany.Email})",
                CurrentClaimOwner = currentOwner,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR).InvestigationCaseSubStatusId,
                UpdatedBy = supervisorUser
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
            var claimsCaseLocation = _context.CaseLocation
            .Include(c => c.ClaimReport)
            .Include(c => c.ClaimsInvestigation)
            .Include(c => c.InvestigationCaseSubStatus)
            .Include(c => c.Vendor)
            .Include(c => c.PinCode)
            .Include(c => c.District)
            .Include(c => c.State)
            .Include(c => c.State)
            .FirstOrDefault(c => c.CaseLocationId == caseLocationId && c.ClaimsInvestigationId == claimsInvestigationId);

            var report = _context.ClaimReport.FirstOrDefault(c => c.ClaimReportId == claimsCaseLocation.ClaimReport.ClaimReportId);
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;

            _context.ClaimReport.Update(report);
            claimsCaseLocation.ClaimReport = report;
            claimsCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR).InvestigationCaseSubStatusId;
            claimsCaseLocation.IsReviewCaseLocation = true;
            _context.CaseLocation.Update(claimsCaseLocation);

            var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(p=>p.ClientCompany)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
            claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
            claimsCaseToAllocateToVendor.UserRoleActionedTo = $"{AppRoles.Creator.GetEnumDisplayName()} ({claimsCaseToAllocateToVendor.PolicyDetail.ClientCompany.Email})";
            claimsCaseToAllocateToVendor.Updated = DateTime.UtcNow;
            claimsCaseToAllocateToVendor.UpdatedBy = userEmail;
            claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
            claimsCaseToAllocateToVendor.IsReviewCase = true;
            claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;
            var existinCaseLocation = claimsCaseToAllocateToVendor.CaseLocations.FirstOrDefault(c => c.CaseLocationId == caseLocationId);
            existinCaseLocation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId;
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
                UserRoleActionedTo= $"{AppRoles.Creator.GetEnumDisplayName()} ({claimsCaseToAllocateToVendor.PolicyDetail.ClientCompany.Email})",
                ClaimsInvestigationId = claimsInvestigationId,
                Created = DateTime.UtcNow,
                Time2Update = DateTime.UtcNow.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR).InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? claimsCaseToAllocateToVendor : null;
        }
    }
}