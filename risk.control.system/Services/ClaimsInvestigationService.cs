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

        Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, SupervisorRemarkType remarks, IFormFile? claimDocument = null);

        Task<ClaimsInvestigation> ProcessCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType);

        List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors);

        Task WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId);

        Task<List<string>> ProcessAutoAllocation(List<string> claims, ClientCompany company, string userEmail);
        Task<bool> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId);
        Task<bool> SubmitNotes(string userEmail, string claimId, string notes);

        Task<ClaimsInvestigation> SubmitQueryToAgency(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument);
        Task<ClaimsInvestigation> SubmitQueryReplyToCompany(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault);
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
                var initiatedStatus = _context.InvestigationCaseStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INITIATED);
                var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

                var assigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

                claimsInvestigation.Updated = DateTime.Now;
                claimsInvestigation.UserEmailActioned = userEmail;
                claimsInvestigation.UserEmailActionedTo = userEmail;
                claimsInvestigation.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
                claimsInvestigation.UpdatedBy = userEmail;
                claimsInvestigation.CurrentUserEmail = userEmail;
                claimsInvestigation.CurrentClaimOwner = currentUser.Email;
                claimsInvestigation.InvestigationCaseStatusId = initiatedStatus.InvestigationCaseStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = create? createdStatus.InvestigationCaseSubStatusId:assigned2AssignerStatus.InvestigationCaseSubStatusId;

                var aaddedClaimId = _context.ClaimsInvestigation.Add(claimsInvestigation);
                var log = new InvestigationTransaction
                {
                    ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                    UserEmailActioned = userEmail,
                    UserEmailActionedTo = userEmail,
                    UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
                    CurrentClaimOwner = currentUser.Email,
                    HopCount = 0,
                    Time2Update = 0,
                    InvestigationCaseStatusId = initiatedStatus.InvestigationCaseStatusId,
                    InvestigationCaseSubStatusId = create ? createdStatus.InvestigationCaseSubStatusId : assigned2AssignerStatus.InvestigationCaseSubStatusId,
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

                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
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
                    var key = Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY");
                    var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=8&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
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
                    claimsInvestigation.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
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
                        UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
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

        public async Task<bool> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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
            claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
            claimsInvestigation.CompanyWithdrawlComment = $"WITHDRAWN: {currentUser.Email} :{model.ClaimsInvestigation.CompanyWithdrawlComment}";
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.ManualNew = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.VendorId = null;
            claimsInvestigation.Vendor = null;

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
                UserRoleActionedTo = $"{company.Email}",
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
                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return company.AutoAllocation;
        }
        public async Task WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.VendorApplicationUser.Include(u=>u.Vendor).FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

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
            claimsInvestigation.AgencyDeclineComment = $"DECLINED: {currentUser.Email} :{model.ClaimsInvestigation.AgencyDeclineComment}";
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.AutoNew = 0;
            claimsInvestigation.VendorId = null;
            claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
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
                UserRoleActionedTo = $"{company.Email}",
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                CurrentClaimOwner = currentUser.Email,
                ClaimsInvestigationId = claimsInvestigation.ClaimsInvestigationId,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = withdrawnByAgency.InvestigationCaseSubStatusId,
                UpdatedBy = currentUser.Email
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            try
            {
                var rows = await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
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
                var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                    .Include(c=>c.PolicyDetail)
                    .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);
                claimsCaseToAllocateToVendor.AssignedToAgency = true;
                claimsCaseToAllocateToVendor.Updated = DateTime.Now;
                claimsCaseToAllocateToVendor.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + " (" + currentUser.Email + ")";
                claimsCaseToAllocateToVendor.CurrentUserEmail = userEmail;
                claimsCaseToAllocateToVendor.CurrentClaimOwner = supervisor.Email;

                claimsCaseToAllocateToVendor.InvestigationCaseSubStatusId = allocatedToVendor.InvestigationCaseSubStatusId;
                claimsCaseToAllocateToVendor.UserEmailActioned = userEmail;
                claimsCaseToAllocateToVendor.UserEmailActionedTo = string.Empty;
                claimsCaseToAllocateToVendor.UserRoleActionedTo = $"{vendor.Email}";
                claimsCaseToAllocateToVendor.Vendors.Add(vendor);
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
                    UserRoleActionedTo = $"{vendor.Email}",
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
                var agentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == vendorAgentEmail);
                claim.UserEmailActioned = currentUser;
                claim.UserEmailActionedTo = agentUser.Email;
                claim.UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agentUser.Vendor.Email})";
                claim.Updated = DateTime.Now;
                claim.UpdatedBy = supervisor.Email;
                claim.CurrentUserEmail = currentUser;
                claim.InvestigateView = 0;
                claim.NotWithdrawable = true;
                claim.NotDeclinable = true;
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
            var agent = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var submitted2Supervisor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var supervisor = await GetSupervisor(agent.VendorId.Value);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            claim.VerifyView = 0;
            claim.InvestigateView = 0;
            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{agent.Vendor.Email}";
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = agent.FirstName + " " + agent.LastName + "(" + agent.Email + ")";
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = supervisor.Email;
            claim.InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId;

            var claimReport = claim.AgencyReport;

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

            var lastLog = _context.InvestigationTransaction.Where(i =>
               i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                                       .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = agent.Email,
                UserRoleActionedTo = $"{agent.Vendor.Email}",
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
                var rows = await _context.SaveChangesAsync();
                return claim;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseLocationId, string claimsInvestigationId, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null)
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, claimsInvestigationId, caseLocationId, supervisorRemarks, reportUpdateStatus, claimDocument);
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
            var rejected = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            try
            {
                var claim = _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.AgencyReport)
                    .Include(p => p.ClientCompany)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                claim.AgencyReport.AssessorRemarkType = assessorRemarkType;
                claim.AgencyReport.AssessorRemarks = assessorRemarks;
                claim.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
                claim.AgencyReport.AssessorEmail = userEmail;

                claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = rejected.InvestigationCaseSubStatusId;
                claim.Updated = DateTime.Now;
                claim.UserEmailActioned = userEmail;
                claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})";
                claim.UserEmailActionedTo = userEmail;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);

                var finalLog = new InvestigationTransaction
                {
                    HopCount = finalHop + 1,
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})",
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
                    AgencyReportId = claim.AgencyReport?.AgencyReportId,
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
                Console.WriteLine(ex.StackTrace);
            }
            return null!;
        }
        private async Task<ClaimsInvestigation> ApproveCaseReport(string userEmail, string assessorRemarks, long caseLocationId, string claimsInvestigationId, AssessorRemarkType assessorRemarkType)
        {
            var approved = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            try
            {
                var claim = _context.ClaimsInvestigation
                    .Include(p => p.PolicyDetail)
                    .Include(p => p.ClientCompany)
                    .Include(c => c.AgencyReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                claim.AgencyReport.AssessorRemarkType = assessorRemarkType;
                claim.AgencyReport.AssessorRemarks = assessorRemarks;
                claim.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
                claim.AgencyReport.AssessorEmail = userEmail;

                claim.InvestigationCaseStatusId = finished.InvestigationCaseStatusId;
                claim.InvestigationCaseSubStatusId = approved.InvestigationCaseSubStatusId;
                claim.Updated = DateTime.Now;
                claim.UserEmailActioned = userEmail;
                claim.UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})";
                claim.UserEmailActionedTo = userEmail;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);

                var finalLog = new InvestigationTransaction
                {
                    HopCount = finalHop + 1,
                    UserEmailActioned = userEmail,
                    UserRoleActionedTo = $"{AppRoles.COMPANY_ADMIN.GetEnumDisplayName()} ({claim.ClientCompany.Email})",
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
                    AgencyReportId = claim.AgencyReport?.AgencyReportId,
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
                Console.WriteLine(ex.StackTrace);
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
            return null!;
        }

        private async Task<ClaimsInvestigation> ReAssignToCreator(string userEmail, string claimsInvestigationId, long caseLocationId, string assessorRemarks, AssessorRemarkType assessorRemarkType)
        {
            var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var claimsCaseToReassign = _context.ClaimsInvestigation
                .Include(c => c.PreviousClaimReports)
                .Include(c => c.AgencyReport)
                .Include(c => c.AgencyReport.DigitalIdReport)
                .Include(c => c.AgencyReport.DocumentIdReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);


            claimsCaseToReassign.AgencyReport.AssessorRemarkType = assessorRemarkType;
            claimsCaseToReassign.AgencyReport.AssessorRemarks = assessorRemarks;
            claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
            claimsCaseToReassign.AgencyReport.AssessorEmail = userEmail;
            var reAssigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var saveReport = new PreviousClaimReport
            {
                ClaimsInvestigationId = claimsInvestigationId,
                AgentEmail = claimsCaseToReassign.AgencyReport.AgentEmail,
                DigitalIdReport = claimsCaseToReassign.AgencyReport.DigitalIdReport,
                DocumentIdReport = claimsCaseToReassign.AgencyReport.DocumentIdReport,
                AgentRemarks = claimsCaseToReassign.AgencyReport.AgentRemarks,
                AgentRemarksUpdated = claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated,
                AssessorEmail = claimsCaseToReassign.AgencyReport.AssessorEmail,
                AssessorRemarks = claimsCaseToReassign.AgencyReport.AssessorRemarks,
                AssessorRemarkType = claimsCaseToReassign.AgencyReport.AssessorRemarkType,
                AssessorRemarksUpdated = claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated,
                ReportQuestionaire = claimsCaseToReassign.AgencyReport.ReportQuestionaire,
                SupervisorEmail = claimsCaseToReassign.AgencyReport.SupervisorEmail,
                SupervisorRemarks = claimsCaseToReassign.AgencyReport.SupervisorRemarks,
                SupervisorRemarksUpdated = claimsCaseToReassign.AgencyReport.SupervisorRemarksUpdated,
                SupervisorRemarkType = claimsCaseToReassign.AgencyReport.SupervisorRemarkType,
                Updated = DateTime.Now,
                UpdatedBy = userEmail,
            };
            var currentSavedReport = _context.PreviousClaimReport.Add(saveReport);

            var newReport = new AgencyReport
            {
                ReportQuestionaire = new ReportQuestionaire(),
                DocumentIdReport = new DocumentIdReport(),
                DigitalIdReport = new DigitalIdReport()
            };
            claimsCaseToReassign.PreviousClaimReports.Add(saveReport);
            claimsCaseToReassign.AgencyReport.DigitalIdReport = new DigitalIdReport();
            claimsCaseToReassign.AgencyReport.DocumentIdReport = new DocumentIdReport();
            claimsCaseToReassign.AgencyReport.ReportQuestionaire = new ReportQuestionaire();

            claimsCaseToReassign.AssignedToAgency = false;
            claimsCaseToReassign.ReviewCount += 1;
            claimsCaseToReassign.UserEmailActioned = userEmail;
            claimsCaseToReassign.UserEmailActionedTo = string.Empty;
            claimsCaseToReassign.UserRoleActionedTo = $"{currentUser.ClientCompany.Email}";
            claimsCaseToReassign.Updated = DateTime.Now;
            claimsCaseToReassign.UpdatedBy = userEmail;
            claimsCaseToReassign.VendorId = null;
            claimsCaseToReassign.CurrentUserEmail = userEmail;
            claimsCaseToReassign.IsReviewCase = true;
            claimsCaseToReassign.AssessView = 0;
            claimsCaseToReassign.ActiveView = 0;
            claimsCaseToReassign.AllocateView = 0;
            claimsCaseToReassign.VerifyView = 0;
            claimsCaseToReassign.AssessView = 0;
            claimsCaseToReassign.ManualNew = 0;
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
                UserRoleActionedTo = $"{currentUser.ClientCompany.Email}",
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

        private async Task<ClaimsInvestigation> ApproveAgentReport(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.Vendor)
                .Include(c => c.ClientCompany)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

            var submitted2Assessor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();
            claim.AssignedToAgency = false;
            claim.IsReviewCase = false;
            claim.UserEmailActioned = userEmail;
            claim.UserEmailActionedTo = string.Empty;
            claim.UserRoleActionedTo = $"{claim.ClientCompany.Email}";
            claim.UserEmailActionedTo = string.Empty;
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = userEmail;
            claim.NotDeclinable = true;
            claim.CurrentUserEmail = userEmail;
            claim.CurrentClaimOwner = userEmail;
            claim.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId;
            claim.InvestigationCaseSubStatusId = submitted2Assessor.InvestigationCaseSubStatusId;

            var report = claim.AgencyReport;
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;
            report.SupervisorRemarksUpdated = DateTime.Now;
            report.SupervisorEmail = userEmail;

            if (claimDocument is not null)
            {
                using var dataStream = new MemoryStream();
                claimDocument.CopyTo(dataStream);
                report.SupervisorAttachment = dataStream.ToArray();
                report.SupervisorFileName = Path.GetFileName(claimDocument.FileName);
                report.SupervisorFileExtension = Path.GetExtension(claimDocument.FileName);
                report.SupervisorFileType = claimDocument.ContentType;
            }
            
            report.Vendor = claim.Vendor;
            _context.AgencyReport.Update(report);
            _context.ClaimsInvestigation.Update(claim);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claim.ClientCompany.Email}",
                CurrentClaimOwner = userEmail,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = submitted2Assessor.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);
            _context.ClaimsInvestigation.Update(claim);
            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task<ClaimsInvestigation> ReAllocateToVendorAgent(string userEmail, string claimsInvestigationId, long caseLocationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            var agencyUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(s => s.Email == userEmail);


            var claimsCaseToAllocateToVendor = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);

            var report = claimsCaseToAllocateToVendor.AgencyReport;
            report.SupervisorRemarkType = reportUpdateStatus;
            report.SupervisorRemarks = supervisorRemarks;

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
                UserRoleActionedTo = $"{claimsCaseToAllocateToVendor.ClientCompany.Email}",
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

        public async Task<ClaimsInvestigation> SubmitQueryToAgency(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.Vendor)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var requestedByAssessor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            claim.InvestigationCaseSubStatusId = requestedByAssessor.InvestigationCaseSubStatusId;
            claim.UpdatedBy = userEmail;
            claim.UserEmailActioned = userEmail;
            claim.AssignedToAgency = true;
            claim.IsQueryCase = true;
            claim.UserRoleActionedTo = $"{claim.Vendor.Email}";

            if (messageDocument != null)
            {
                using var ms = new MemoryStream();
                messageDocument.CopyTo(ms);
                request.QuestionImageAttachment = ms.ToArray();
                request.QuestionImageFileName = Path.GetFileName(messageDocument.FileName);
                request.QuestionImageFileExtension = Path.GetExtension(messageDocument.FileName);
                request.QuestionImageFileType = messageDocument.ContentType;
            }
            claim.AgencyReport.EnquiryRequest = request;
            claim.AgencyReport.Updated = DateTime.Now;
            claim.AgencyReport.UpdatedBy = userEmail;
            claim.AgencyReport.EnquiryRequest.Updated = DateTime.Now;
            claim.AgencyReport.EnquiryRequest.UpdatedBy = userEmail;
            _context.QueryRequest.Update(request);
            _context.ClaimsInvestigation.Update(claim);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claim.ClaimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claim.Vendor.Email}",
                CurrentClaimOwner = userEmail,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = requestedByAssessor.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ClaimsInvestigation> SubmitQueryReplyToCompany(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var replyByAgency = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);
            var lastLog = _context.InvestigationTransaction.Where(i =>
                i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            claim.InvestigationCaseSubStatusId = replyByAgency.InvestigationCaseSubStatusId;
            claim.UpdatedBy = userEmail;
            claim.UserEmailActioned = userEmail;
            claim.AssignedToAgency = false;
            claim.AssessView = 0;
            claim.UserRoleActionedTo = $"{claim.ClientCompany.Email}";
            var enquiryRequest = claim.AgencyReport.EnquiryRequest;
            enquiryRequest.Answer = request.Answer;
            if (flexRadioDefault[0] == "a")
            {
                enquiryRequest.AnswerSelected = enquiryRequest.AnswerA;
            }
            else if (flexRadioDefault[0] == "b")
            {
                enquiryRequest.AnswerSelected = enquiryRequest.AnswerB;
            }
            else if (flexRadioDefault[0] == "c")
            {
                enquiryRequest.AnswerSelected = enquiryRequest.AnswerC;
            }

            else if (flexRadioDefault[0] == "d")
            {
                enquiryRequest.AnswerSelected = enquiryRequest.AnswerD;
            }

            enquiryRequest.Updated = DateTime.Now;
            enquiryRequest.UpdatedBy = userEmail;

            if (messageDocument != null)
            {
                using var ms = new MemoryStream();
                messageDocument.CopyTo(ms);
                enquiryRequest.AnswerImageAttachment = ms.ToArray();
                enquiryRequest.AnswerImageFileName = Path.GetFileName(messageDocument.FileName);
                enquiryRequest.AnswerImageFileExtension = Path.GetExtension(messageDocument.FileName);
                enquiryRequest.AnswerImageFileType = messageDocument.ContentType;
            }

            claim.AgencyReport.EnquiryRequests.Add(enquiryRequest);

            _context.QueryRequest.Update(enquiryRequest);
            claim.AgencyReport.EnquiryRequests.Add(enquiryRequest);
            _context.ClaimsInvestigation.Update(claim);

            var lastLogHop = _context.InvestigationTransaction
                                       .Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId)
                .AsNoTracking().Max(s => s.HopCount);

            var log = new InvestigationTransaction
            {
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claim.ClaimsInvestigationId,
                UserEmailActioned = userEmail,
                UserRoleActionedTo = $"{claim.Vendor.Email}",
                CurrentClaimOwner = userEmail,
                Created = DateTime.Now,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS).InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = replyByAgency.InvestigationCaseSubStatusId,
                UpdatedBy = userEmail
            };
            _context.InvestigationTransaction.Add(log);

            try
            {
                return await _context.SaveChangesAsync() > 0 ? claim : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> SubmitNotes(string userEmail, string claimId, string notes)
        {
            var claim = _context.ClaimsInvestigation
               .Include(c => c.ClaimNotes)
               .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            claim.ClaimNotes.Add(new ClaimNote
            {
                 Comment = notes,
                 Sender = userEmail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                UpdatedBy = userEmail
            });
            _context.ClaimsInvestigation.Update(claim);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}