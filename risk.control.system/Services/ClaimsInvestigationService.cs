using AspNetCoreHero.ToastNotification.Notyf;

using Hangfire;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Company;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        Task AssignToAssigner(string userEmail, List<string> claimsInvestigations, string url ="");
        Task<int> UpdateCaseAllocationStatus(string userEmail, List<string> claimsInvestigations);

        Task<(string, string)> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, bool AutoAllocated = true);

        Task<ClaimsInvestigation> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, string claimsInvestigationId);

        Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4);

        Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, string claimsInvestigationId, SupervisorRemarkType remarks, IFormFile? claimDocument = null, string editRemarks = "");

        Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary);

        List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors);
        List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors);

        Task<Vendor> WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId);

        Task<string> ProcessAutoAllocation(string claim, string userEmail, string url = "");
        Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId);
        Task<bool> SubmitNotes(string userEmail, string claimId, string notes);

        Task<ClaimsInvestigation> SubmitQueryToAgency(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument);
        Task<ClaimsInvestigation> SubmitQueryReplyToCompany(string userEmail, string claimId, EnquiryRequest request, IFormFile messageDocument, List<string> flexRadioDefault);
        Task BackgroundAutoAllocation(List<string> claims, string userEmail, string url="");
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext _context;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor accessor;
        private readonly IPdfReportService reportService;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IProgressService progressService;
        private readonly ICustomApiCLient customApiCLient;

        public ClaimsInvestigationService(ApplicationDbContext context,
            IHttpContextAccessor accessor,
            IPdfReportService reportService,
            IBackgroundJobClient backgroundJobClient,
            IProgressService progressService,
            ICustomApiCLient customApiCLient,
            IMailboxService mailboxService,
            IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.accessor = accessor;
            this.reportService = reportService;
            this.backgroundJobClient = backgroundJobClient;
            this.progressService = progressService;
            this.customApiCLient = customApiCLient;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
        }
        [AutomaticRetry(Attempts = 0)]
        public async Task BackgroundAutoAllocation(List<string> claimIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(claimIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = claimIds.Except(autoAllocatedCases)?.ToList();
            
            if (claimIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);

            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(userEmail,autoAllocatedCases, notAutoAllocated, url));
        }
        async Task<List<string>> DoAutoAllocation(List<string> claims, string userEmail, string url = "")
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var uploadedRecordsCount = 0;

            var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var claimTasks = claims.Select(async claim =>
            {
                int progress = (int)(((uploadedRecordsCount + 1) / (double)claims.Count) * 100);
                progressService.UpdateAssignmentProgress(claim, progress);

                // 1. Fetch Claim Details & Pincode in Parallel
                var claimsInvestigation = await _context.ClaimsInvestigation
                    .AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .ThenInclude(c => c.LineOfBusiness)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                        .ThenInclude(c => c.PinCode)
                    .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claim);

                if(claimsInvestigation == null || !claimsInvestigation.IsValidCaseData()) return null; // Handle missing claim

                string pinCode2Verify = claimsInvestigation.PolicyDetail?.LineOfBusiness.Name.ToLower() == UNDERWRITING
                    ? claimsInvestigation.CustomerDetail?.PinCode?.Code
                    : claimsInvestigation.BeneficiaryDetail?.PinCode?.Code;

                var pincodeDistrictState = await _context.PinCode
                    .AsNoTracking()
                    .Include(d => d.District)
                    .Include(s => s.State)
                    .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

                // 2. Find Vendors Using LINQ
                var distinctVendorIds = company.EmpanelledVendors
                    .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                        serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                        serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId &&
                        (serviceType.StateId == pincodeDistrictState.StateId &&
                         (serviceType.DistrictId == null || serviceType.DistrictId == pincodeDistrictState.DistrictId))
                    ))
                    .Select(v => v.VendorId) // Select only VendorId
                    .Distinct() // Ensure uniqueness
                    .ToList();

                if (!distinctVendorIds.Any()) return null; // No vendors found, skip this claim

                // 3. Get Vendor Load & Allocate
                var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
                    .OrderBy(o => o.CaseCount)
                    .ToList();

                var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
                if (selectedVendorId == null) return null; // No vendors available

                var (policy, status) = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    return null;
                }
                var jobId = backgroundJobClient.Enqueue(() => 
                    mailboxService.NotifyClaimAllocationToVendor(userEmail, policy, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId, url));

                return claim; // Return allocated claim
            });

            var results = await Task.WhenAll(claimTasks); // Run all tasks in parallel
            return results.Where(r => r != null).ToList(); // Remove nulls and return allocated claims
        }
        public async Task<string> ProcessAutoAllocation(string claim, string userEmail, string url = "")
        {
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);

            var company = _context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            // 1. Fetch Claim Details & Pincode in Parallel
            var claimsInvestigation = await _context.ClaimsInvestigation
                .AsNoTracking()
                .Include(c => c.PolicyDetail)
                    .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == claim);

            string pinCode2Verify = claimsInvestigation.PolicyDetail?.LineOfBusiness.Name.ToLower() == UNDERWRITING
                ? claimsInvestigation.CustomerDetail?.PinCode?.Code
                : claimsInvestigation.BeneficiaryDetail?.PinCode?.Code;

            var pincodeDistrictState = await _context.PinCode
                .AsNoTracking()
                .Include(d => d.District)
                .Include(s => s.State)
                .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

            // 2. Find Vendors Using LINQ
            var distinctVendorIds = company.EmpanelledVendors
                .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                    serviceType.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId &&
                    serviceType.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId &&
                    (serviceType.StateId == pincodeDistrictState.StateId &&
                     (serviceType.DistrictId == null || serviceType.DistrictId == pincodeDistrictState.DistrictId))
                ))
                .Select(v => v.VendorId) // Select only VendorId
                .Distinct() // Ensure uniqueness
                .ToList();

            if (!distinctVendorIds.Any()) return null; // No vendors found, skip this claim

            // 3. Get Vendor Load & Allocate
            var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
                .OrderBy(o => o.CaseCount)
                .ToList();

            var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
            if (selectedVendorId == null) return null; // No vendors available

            var (policy, status) = await AllocateToVendor(userEmail, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId);

            if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
            {
                await AssignToAssigner(userEmail, new List<string> { claim });
                var job = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToAssigner(userEmail, new List<string> { claim },url));
                return null;
            }

            // 4. Send Notification in Background
            var jobId = backgroundJobClient.Enqueue(() =>
                mailboxService.NotifyClaimAllocationToVendor(userEmail, policy, claimsInvestigation.ClaimsInvestigationId, selectedVendorId.VendorId, url));

            return claimsInvestigation.PolicyDetail.ContractNumber; // Return allocated claim
        }
        public List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors)
        {
            // Get relevant status IDs in one query
            var relevantStatuses = _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                }.Contains(i.Name.ToUpper()))
                .Select(i => i.InvestigationCaseSubStatusId)
                .ToHashSet(); // Improves lookup performance

            // Fetch cases that match the criteria
            var vendorCaseCount = _context.ClaimsInvestigation
                .Where(c => !c.Deleted &&
                            c.VendorId.HasValue &&
                            c.AssignedToAgency &&
                            relevantStatuses.Contains(c.InvestigationCaseSubStatusId))
                .GroupBy(c => c.VendorId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Create the list of VendorIdWithCases
            return existingVendors
                .Select(vendorId => new VendorIdWithCases
                {
                    VendorId = vendorId,
                    CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0)
                })
                .ToList();
        }

        public List<VendorCaseModel> GetAgencyLoad(List<Vendor> existingVendors)
        {
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var requestedByAssessor = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var claimsCases = _context.ClaimsInvestigation
                .Where(c =>
                !c.Deleted &&
                c.VendorId.HasValue &&
                c.AssignedToAgency &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == requestedByAssessor.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                );

            var vendorCaseCount = claimsCases
                .Where(c => c.VendorId.HasValue)
                .GroupBy(c => c.VendorId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var vendorWithCaseCounts = existingVendors
                .Select(vendor => new VendorCaseModel
                {
                    Vendor = vendor,
                    CaseCount = vendorCaseCount.GetValueOrDefault(vendor.VendorId, 0)
                })
                .ToList();

            return vendorWithCaseCounts;

        }

        public async Task AssignToAssigner(string userEmail, List<string> claims, string url = "")
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
                    claimsInvestigation.STATUS = ALLOCATION_STATUS.READY;
                    claimsInvestigation.IsReady2Assign = true;
                    claimsInvestigation.CREATEDBY = CREATEDBY.MANUAL;
                    claimsInvestigation.AutoAllocated = false;
                    claimsInvestigation.ActiveView = 0;
                    claimsInvestigation.ManualNew = 0;
                    claimsInvestigation.AllocateView = 0;
                    claimsInvestigation.AutoNew = 0;
                    claimsInvestigation.VendorId = null;
                    claimsInvestigation.Vendor = null;

                    claimsInvestigation.InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId;
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
                        UpdatedBy = currentUser.Email,
                        Updated = DateTime.Now
                    };
                    _context.InvestigationTransaction.Add(log);
                }
                _context.UpdateRange(cases2Assign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var vendorId = claimsInvestigation.VendorId;
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);

            claimsInvestigation.Updated = DateTime.Now;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.STATUS = ALLOCATION_STATUS.READY;
            claimsInvestigation.AssignedToAgency = false;
            claimsInvestigation.CurrentClaimOwner = userEmail;
            claimsInvestigation.UserEmailActioned = userEmail;
            claimsInvestigation.UserEmailActionedTo = userEmail;
            claimsInvestigation.UserRoleActionedTo = $"{company.Email}";
            claimsInvestigation.CompanyWithdrawlComment = $"WITHDRAWN: {currentUser.Email} :{model.ClaimsInvestigation.CompanyWithdrawlComment}";
            claimsInvestigation.ActiveView = 0;
            claimsInvestigation.ManualNew = 0;
            claimsInvestigation.AllocateView = 0;
            claimsInvestigation.AutoNew = 0;
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
                UpdatedBy = currentUser.Email,
                Updated = DateTime.Now
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
            return (company, vendorId.GetValueOrDefault());
        }

        public async Task<Vendor> WithdrawCase(string userEmail, ClaimTransactionModel model, string claimId)
        {
            var currentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == userEmail);
            var claimsInvestigation = _context.ClaimsInvestigation
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claimsInvestigation.ClientCompanyId);

            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);

            claimsInvestigation.Updated = DateTime.Now;
            claimsInvestigation.UpdatedBy = currentUser.FirstName + " " + currentUser.LastName + "( " + currentUser.Email + ")";
            claimsInvestigation.CurrentUserEmail = userEmail;
            claimsInvestigation.STATUS = ALLOCATION_STATUS.READY;
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
                UpdatedBy = currentUser.Email,
                Updated = DateTime.Now
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
            return currentUser.Vendor;
        }

        public async Task<(string, string)> AllocateToVendor(string userEmail, string claimsInvestigationId, long vendorId, bool autoAllocated = true)
        {
            // Fetch vendor & user details
            var vendor = await _context.Vendor.FindAsync(vendorId);
            var currentUser = await _context.ClientCompanyApplicationUser
                .Include(c => c.ClientCompany)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (vendor == null || currentUser == null) return (string.Empty, string.Empty); // Handle missing data

            // Fetch case statuses in one query
            var caseStatuses = await _context.InvestigationCaseStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.INPROGRESS
                }.Contains(i.Name))
                .ToDictionaryAsync(i => i.Name, i => i.InvestigationCaseStatusId);

            var subStatuses = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR
                }.Contains(i.Name))
                .ToDictionaryAsync(i => i.Name, i => i.InvestigationCaseSubStatusId);

            if (!caseStatuses.TryGetValue(CONSTANTS.CASE_STATUS.INPROGRESS, out var inProgressId) ||
                !subStatuses.TryGetValue(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR, out var allocatedToVendorId))
            {
                return (string.Empty, string.Empty); // Handle missing status/substatus
            }

            // Fetch case
            var claimsCase = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(v => v.ClaimsInvestigationId == claimsInvestigationId);

            if (claimsCase == null) return (string.Empty, string.Empty); // Handle missing case

            // Update case details
            claimsCase.STATUS = ALLOCATION_STATUS.COMPLETED;
            claimsCase.AssignedToAgency = true;
            claimsCase.Updated = DateTime.Now;
            claimsCase.UpdatedBy = $"{currentUser.FirstName} {currentUser.LastName} ({currentUser.Email})";
            claimsCase.CurrentUserEmail = userEmail;
            claimsCase.EnablePassport = currentUser.ClientCompany.EnablePassport;
            claimsCase.AiEnabled = currentUser.ClientCompany.AiEnabled;
            claimsCase.EnableMedia = currentUser.ClientCompany.EnableMedia;
            claimsCase.InvestigationCaseSubStatusId = allocatedToVendorId;
            claimsCase.UserEmailActioned = userEmail;
            claimsCase.UserEmailActionedTo = string.Empty;
            claimsCase.UserRoleActionedTo = vendor.Email;
            claimsCase.VendorId = vendorId;
            claimsCase.AllocateView = 0;
            claimsCase.AutoAllocated = autoAllocated;
            claimsCase.AllocatedToAgencyTime = DateTime.Now;
            claimsCase.CreatorSla = currentUser.ClientCompany.CreatorSla;
            claimsCase.AssessorSla = currentUser.ClientCompany.AssessorSla;
            claimsCase.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
            claimsCase.AgentSla = currentUser.ClientCompany.AgentSla;
            claimsCase.UpdateAgentReport = currentUser.ClientCompany.UpdateAgentReport;
            claimsCase.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;

            claimsCase.Vendors.Add(vendor); // Ensures relationship update
            _context.ClaimsInvestigation.Update(claimsCase);

            // Get last transaction log
            var lastLog = await _context.InvestigationTransaction
                .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .OrderByDescending(o => o.Created)
                .FirstOrDefaultAsync();

            var lastLogHop = await _context.InvestigationTransaction
                .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                .AsNoTracking()
                .MaxAsync(s => (int?)s.HopCount) ?? 0;

            // Calculate time elapsed
            string timeElapsed = GetTimeElaspedFromLog(lastLog);

            // Create new transaction log
            var log = new InvestigationTransaction
            {
                UserEmailActioned = userEmail,
                UserRoleActionedTo = vendor.Email,
                UserEmailActionedTo = string.Empty,
                HopCount = lastLogHop + 1,
                ClaimsInvestigationId = claimsInvestigationId,
                CurrentClaimOwner = claimsCase.CurrentClaimOwner,
                Time2Update = lastLog != null ? (DateTime.Now - lastLog.Created).Days : 0,
                InvestigationCaseStatusId = inProgressId,
                InvestigationCaseSubStatusId = allocatedToVendorId,
                UpdatedBy = currentUser.Email,
                Updated = DateTime.Now,
                TimeElapsed = timeElapsed
            };

            _context.InvestigationTransaction.Add(log);

            // Save changes
            await _context.SaveChangesAsync();

            return (claimsCase.PolicyDetail.ContractNumber, claimsCase.InvestigationCaseSubStatus.Name);
        }

        public async Task<ClaimsInvestigation> AssignToVendorAgent(string vendorAgentEmail, string currentUser, long vendorId, string claimsInvestigationId)
        {
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var assignedToAgent = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claim = _context.ClaimsInvestigation.Include(c => c.PolicyDetail).Include(c => c.AgencyReport)
                .Include(c => c.CustomerDetail).ThenInclude(c => c.PinCode).Include(c => c.BeneficiaryDetail)
                .Where(c => c.ClaimsInvestigationId == claimsInvestigationId).FirstOrDefault();
            var agentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(u => u.Email == vendorAgentEmail);
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            string drivingDistance, drivingDuration, drivingMap;
            float distanceInMeters;
            int durationInSeconds;
            string LocationLatitude = string.Empty;
            string LocationLongitude = string.Empty;
            if (claim.PolicyDetail?.LineOfBusinessId == underWritingLineOfBusiness)
            {
                LocationLatitude = claim.CustomerDetail?.Latitude;
                LocationLongitude = claim.CustomerDetail?.Longitude;
            }
            else
            {
                LocationLatitude = claim.BeneficiaryDetail?.Latitude;
                LocationLongitude = claim.BeneficiaryDetail?.Longitude;
            }
            (drivingDistance, distanceInMeters, drivingDuration, durationInSeconds, drivingMap) = await customApiCLient.GetMap(double.Parse(agentUser.AddressLatitude), double.Parse(agentUser.AddressLongitude), double.Parse(LocationLatitude), double.Parse(LocationLongitude));
            claim.UserEmailActioned = currentUser;
            claim.UserEmailActionedTo = agentUser.Email;
            claim.UserRoleActionedTo = $"{AppRoles.AGENT.GetEnumDisplayName()} ({agentUser.Vendor.Email})";
            claim.Updated = DateTime.Now;
            claim.UpdatedBy = currentUser;
            claim.CurrentUserEmail = currentUser;
            claim.InvestigateView = 0;
            claim.NotWithdrawable = true;
            claim.NotDeclinable = true;
            claim.CurrentClaimOwner = agentUser.Email;
            claim.InvestigationCaseSubStatusId = assignedToAgent.InvestigationCaseSubStatusId;
            claim.SelectedAgentDrivingDistance = drivingDistance;
            claim.SelectedAgentDrivingDuration = drivingDuration;
            claim.SelectedAgentDrivingDistanceInMetres = distanceInMeters;
            claim.SelectedAgentDrivingDurationInSeconds = durationInSeconds;
            claim.SelectedAgentDrivingMap = drivingMap;
            claim.TaskToAgentTime = DateTime.Now;
            var lastLog = _context.InvestigationTransaction.Where(i =>
            i.ClaimsInvestigationId == claim.ClaimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

            var lastLogHop = _context.InvestigationTransaction.Where(i => i.ClaimsInvestigationId == claim.ClaimsInvestigationId).AsNoTracking().Max(s => s.HopCount);
            string timeElapsed = GetTimeElaspedFromLog(lastLog);

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
                UpdatedBy = currentUser,
                Updated = DateTime.Now,
                TimeElapsed = timeElapsed
            };
            _context.InvestigationTransaction.Add(log);


            var claimsLineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIMS).LineOfBusinessId;

            var isClaim = claim.PolicyDetail.LineOfBusinessId == claimsLineOfBusinessId;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date and time of death ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            _context.ClaimsInvestigation.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, string claimsInvestigationId, string remarks, string? answer1, string? answer2, string? answer3, string? answer4)
        {
            var agent = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(a => a.Email.Trim().ToLower() == userEmail.ToLower());
            var inProgress = _context.InvestigationCaseStatus.FirstOrDefault(
                                   i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var submitted2Supervisor = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
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
            claim.CurrentClaimOwner = userEmail;
            claim.InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId;
            claim.SubmittedToSupervisorTime = DateTime.Now;
            var claimReport = claim.AgencyReport;

            claimReport.ReportQuestionaire.Answer1 = answer1;
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
                CurrentClaimOwner = userEmail,
                Time2Update = DateTime.Now.Subtract(lastLog.Created).Days,
                InvestigationCaseStatusId = inProgress.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = submitted2Supervisor.InvestigationCaseSubStatusId,
                UpdatedBy = agent.Email,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            _context.ClaimsInvestigation.Update(claim);

            try
            {
                var rows = await _context.SaveChangesAsync();
                return (agent.Vendor, claim.PolicyDetail.ContractNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ClaimsInvestigation> ProcessAgentReport(string userEmail, string supervisorRemarks, string claimsInvestigationId, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "")
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, claimsInvestigationId, supervisorRemarks, reportUpdateStatus, claimDocument, editRemarks);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAllocateToVendorAgent(userEmail, claimsInvestigationId, supervisorRemarks, reportUpdateStatus);
            }
        }

        public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
        {
            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                return await ApproveCaseReport(userEmail, assessorRemarks, claimsInvestigationId, reportUpdateStatus, reportAiSummary);
            }
            else if (reportUpdateStatus == AssessorRemarkType.REJECT)
            {
                //PUT th case back in review list :: Assign back to Agent
                return await RejectCaseReport(userEmail, assessorRemarks, claimsInvestigationId, reportUpdateStatus, reportAiSummary);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAssignToCreator(userEmail, claimsInvestigationId, assessorRemarks, reportUpdateStatus, reportAiSummary);
            }
        }

        private async Task<(ClientCompany, string)> RejectCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var rejected = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            try
            {
                var claim = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.AgencyReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                claim.AgencyReport.AiSummary = reportAiSummary;
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
                claim.ProcessedByAssessorTime = DateTime.Now;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);
                var lastLog = _context.InvestigationTransaction.Where(i =>
              i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

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
                    UpdatedBy = userEmail,
                    Updated = DateTime.Now,
                    TimeElapsed = GetTimeElaspedFromLog(lastLog)
                };

                _context.InvestigationTransaction.Add(finalLog);

                var saveCount = await _context.SaveChangesAsync();

                backgroundJobClient.Enqueue(() => reportService.Run(userEmail, claimsInvestigationId));

                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (null!, string.Empty);
        }

        private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, string claimsInvestigationId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var approved = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var finished = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            try
            {
                var claim = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.AgencyReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

                claim.AgencyReport.AiSummary = reportAiSummary;
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
                claim.ProcessedByAssessorTime = DateTime.Now;
                _context.ClaimsInvestigation.Update(claim);

                var finalHop = _context.InvestigationTransaction
                                   .Where(i => i.ClaimsInvestigationId == claimsInvestigationId)
                                    .AsNoTracking().Max(s => s.HopCount);
                var lastLog = _context.InvestigationTransaction.Where(i =>
                             i.ClaimsInvestigationId == claimsInvestigationId).OrderByDescending(o => o.Created)?.FirstOrDefault();

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
                    UpdatedBy = userEmail,
                    Updated = DateTime.Now,
                    TimeElapsed = GetTimeElaspedFromLog(lastLog)
                };

                _context.InvestigationTransaction.Add(finalLog);

                var saveCount = await _context.SaveChangesAsync();

                var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

                backgroundJobClient.Enqueue(() => reportService.Run(userEmail, claimsInvestigationId));

                return saveCount > 0 ? (currentUser.ClientCompany, claim.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (null!, string.Empty);
        }

        private async Task<(ClientCompany, string)> ReAssignToCreator(string userEmail, string claimsInvestigationId, string assessorRemarks, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var currentUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var claimsCaseToReassign = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.AgencyReport.DigitalIdReport)
                .Include(c => c.AgencyReport.PanIdReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.PolicyDetail)
                .FirstOrDefault(v => v.ClaimsInvestigationId == claimsInvestigationId);


            claimsCaseToReassign.AgencyReport.AiSummary = reportAiSummary;
            claimsCaseToReassign.AgencyReport.AssessorRemarkType = assessorRemarkType;
            claimsCaseToReassign.AgencyReport.AssessorRemarks = assessorRemarks;
            claimsCaseToReassign.AgencyReport.AssessorRemarksUpdated = DateTime.Now;
            claimsCaseToReassign.ReviewByAssessorTime = DateTime.Now;
            claimsCaseToReassign.AgencyReport.AssessorEmail = userEmail;
            var reAssigned = _context.InvestigationCaseSubStatus.FirstOrDefault(
                    i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var newReport = new AgencyReport
            {
                ReportQuestionaire = new ReportQuestionaire(),
                PanIdReport = new DocumentIdReport(),
                PassportIdReport = new DocumentIdReport(),
                DigitalIdReport = new DigitalIdReport()
            };
            claimsCaseToReassign.AgencyReport.DigitalIdReport = new DigitalIdReport();
            claimsCaseToReassign.AgencyReport.PanIdReport = new DocumentIdReport();
            claimsCaseToReassign.AgencyReport.PassportIdReport = new DocumentIdReport();
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
            claimsCaseToReassign.ProcessedByAssessorTime = DateTime.Now;
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
                CurrentClaimOwner = currentUser.Email,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
            };
            _context.InvestigationTransaction.Add(log);

            return await _context.SaveChangesAsync() > 0 ? (currentUser.ClientCompany, claimsCaseToReassign.PolicyDetail.ContractNumber) : (null!, string.Empty);
        }

        private async Task<ClaimsInvestigation> ApproveAgentReport(string userEmail, string claimsInvestigationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus, IFormFile? claimDocument = null, string editRemarks = "")
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
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
            claim.SubmittedToAssessorTime = DateTime.Now;
            var report = claim.AgencyReport;
            var edited = report.AgentRemarks.Trim() != editRemarks.Trim();
            if (edited)
            {
                report.AgentRemarksEdit = editRemarks;
                report.AgentRemarksEditUpdated = DateTime.Now;
            }

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
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog),
                AgentAnswerEdited = edited
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

        private async Task<ClaimsInvestigation> ReAllocateToVendorAgent(string userEmail, string claimsInvestigationId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
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
            claimsCaseToAllocateToVendor.TaskToAgentTime = DateTime.Now;
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
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
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
            claim.EnquiredByAssessorTime = DateTime.Now;
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
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
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
            claim.EnquiryReplyByAssessorTime = DateTime.Now;
            claim.SubmittedToAssessorTime = DateTime.Now;
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
                UpdatedBy = userEmail,
                Updated = DateTime.Now,
                TimeElapsed = GetTimeElaspedFromLog(lastLog)
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

        private string GetTimeElaspedFromLog(InvestigationTransaction lastLog)
        {
            string timeElapsed = string.Empty;
            if (DateTime.Now.Subtract(lastLog.Created).Days >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Days.ToString() + " days";
            }
            else if (DateTime.Now.Subtract(lastLog.Created).Hours >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Hours.ToString() + " hours";
            }
            else if (DateTime.Now.Subtract(lastLog.Created).Minutes >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Minutes.ToString() + " minutes";
            }
            else if (DateTime.Now.Subtract(lastLog.Created).Seconds >= 1)
            {
                timeElapsed = DateTime.Now.Subtract(lastLog.Created).Seconds.ToString() + " seconds";
            }
            else
            {
                timeElapsed = "Just Now";
            }
            return timeElapsed;
        }

        public async Task<int> UpdateCaseAllocationStatus(string userEmail, List<string> claimsInvestigations)
        {
            try
            {
                if (claimsInvestigations == null || !claimsInvestigations.Any())
                    return 0; // No cases to update

                // Fetch all matching cases in one query
                var cases = await _context.ClaimsInvestigation
                    .Where(v => claimsInvestigations.Contains(v.ClaimsInvestigationId))
                    .ToListAsync();

                if (!cases.Any())
                    return 0; // No matching cases found

                // Update the status only for cases that are not already PENDING
                foreach (var claimsCase in cases)
                {
                    if (claimsCase.STATUS != ALLOCATION_STATUS.PENDING)
                    {
                        claimsCase.STATUS = ALLOCATION_STATUS.PENDING;
                        claimsCase.UpdatedBy = userEmail;
                        claimsCase.Updated = DateTime.Now;
                    }
                }

                _context.ClaimsInvestigation.UpdateRange(cases);
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error properly instead of just rethrowing
                Console.WriteLine("Error updating case allocation status", ex);
                throw;
            }
        }

    }
}