using Hangfire;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services
{
    public interface IAssignCaseService
    {
        Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> caseIds);

        Task BackgroundAutoAllocation(List<long> claims, string userEmail, string url = "");

        Task<string> ProcessAutoSingleAllocation(long caseId, string userEmail, string url = "");

        Task<(string, string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true);
    }

    internal class AssignCaseService : IAssignCaseService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IAgencyCaseLoadService _agencyCaseLoadService;
        private readonly ILogger<AssignCaseService> logger;
        private readonly IMailService mailboxService;
        private readonly ITimelineService timelineService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public AssignCaseService(IDbContextFactory<ApplicationDbContext> contextFactory,
            IAgencyCaseLoadService agencyCaseLoadService,
            ILogger<AssignCaseService> logger,
            IMailService mailboxService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            _contextFactory = contextFactory;
            this._agencyCaseLoadService = agencyCaseLoadService;
            this.logger = logger;
            this.mailboxService = mailboxService;
            this.timelineService = timelineService;
            this.backgroundJobClient = backgroundJobClient;
        }

        public async Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> caseIds)
        {
            try
            {
                if (caseIds == null || !caseIds.Any())
                    return 0; // No cases to update
                await using var context = await _contextFactory.CreateDbContextAsync();
                // Fetch all matching cases in one query
                var cases = await context.Investigations
                    .Where(v => caseIds.Contains(v.Id))
                    .ToListAsync();

                if (!cases.Any())
                    return 0; // No matching cases found

                // Update the status only for cases that are not already PENDING
                foreach (var claimsCase in cases)
                {
                    claimsCase.UpdatedBy = userEmail;
                    claimsCase.Updated = DateTime.UtcNow;
                }

                context.Investigations.UpdateRange(cases);
                return await context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred. {UserEmail}", userEmail);
                throw;
            }
        }

        [AutomaticRetry(Attempts = 1)]
        public async Task BackgroundAutoAllocation(List<long> caseIds, string userEmail, string url = "")
        {
            try
            {
                var autoAllocatedCases = await DoAutoAllocation(caseIds, userEmail, url); // Run all tasks in parallel

                var notAutoAllocated = caseIds.Except(autoAllocatedCases)?.ToList();

                if (caseIds.Count > autoAllocatedCases.Count)
                {
                    await AssignToAssigner(userEmail, notAutoAllocated, url);
                }
                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred. {UserEmail}", userEmail);
                throw;
            }
        }

        private async Task<List<long>> DoAutoAllocation(List<long> claims, string userEmail, string url = "")
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

            var company = await _context.ClientCompany.AsNoTracking()
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            // 1. Pre-fetch initial load for ALL eligible vendors once to save DB hits
            var allVendorIds = company.EmpanelledVendors.Select(v => v.VendorId).ToList();
            var initialLoads = await _agencyCaseLoadService.GetAgencyIdsLoad(allVendorIds);

            // Create a local thread-safe dictionary to track "Work-in-Progress" load
            var localLoadTracker = initialLoads.ToDictionary(v => v.VendorId, v => v.CaseCount);
            var allocationLock = new object();
            var allocatedClaims = new List<long>();

            var claimTasks = claims.Select(async claim =>
            {
                await using var db = await _contextFactory.CreateDbContextAsync();
                var caseTask = await db.Investigations.AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail).ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail).ThenInclude(c => c.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == claim);

                if (caseTask == null || !caseTask.IsValidCaseData()) return 0;

                // ... [Pincode lookup logic remains the same] ...
                var pinCode2Verify = caseTask.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING
                    ? caseTask.CustomerDetail?.PinCode?.Code
                    : caseTask.BeneficiaryDetail?.PinCode?.Code;

                var pincodeDistrictState = await db.PinCode.AsNoTracking()
                    .Include(d => d.District).Include(s => s.State)
                    .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

                // 2. Filter Eligible Vendors
                var eligibleVendorIds = company.EmpanelledVendors
                    .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                        serviceType.InvestigationServiceTypeId == caseTask.PolicyDetail.InvestigationServiceTypeId &&
                        serviceType.InsuranceType == caseTask.PolicyDetail.InsuranceType &&
                        (serviceType.StateId == pincodeDistrictState.StateId &&
                         (serviceType.SelectedDistrictIds?.Contains(-1) == true || serviceType.SelectedDistrictIds.Contains(pincodeDistrictState.DistrictId.Value)))
                    ))
                    .Select(v => v.VendorId).ToList();

                if (!eligibleVendorIds.Any()) return 0;

                // 3. THREAD-SAFE SELECTION: Pick the vendor with the lowest CURRENT load
                long selectedVendorId;
                lock (allocationLock)
                {
                    selectedVendorId = eligibleVendorIds
                        .OrderBy(id => localLoadTracker[id])
                        .First();

                    // Increment local tracker immediately so the next Task sees this vendor as busier
                    localLoadTracker[selectedVendorId]++;
                }

                // 4. Perform the actual DB update
                var (policy, status, _) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId);

                if (!string.IsNullOrEmpty(policy))
                {
                    backgroundJobClient.Enqueue(() =>
                        mailboxService.NotifyCaseAllocationToVendor(userEmail, policy, caseTask.Id, selectedVendorId, url));
                    return claim;
                }

                return 0;
            });

            var results = await Task.WhenAll(claimTasks);
            return results.Where(r => r != 0).ToList();
        }

        public async Task<string> ProcessAutoSingleAllocation(long caseId, string userEmail, string url = "")
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var companyUser = await context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

                var company = await context.ClientCompany.AsNoTracking()
                        .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                        .ThenInclude(e => e.VendorInvestigationServiceTypes)
                        .ThenInclude(v => v.District)
                        .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                // 1. Fetch Claim Details & Pincode in Parallel
                var caseTask = await context.Investigations
                    .AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                        .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                        .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                var pinCode2Verify = caseTask.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING
                    ? caseTask.CustomerDetail?.PinCode?.Code
                    : caseTask.BeneficiaryDetail?.PinCode?.Code;

                var pincodeDistrictState = await context.PinCode
                    .AsNoTracking()
                    .Include(d => d.District)
                    .Include(s => s.State)
                    .FirstOrDefaultAsync(p => p.Code == pinCode2Verify);

                // 2. Find Vendors Using LINQ
                var distinctVendorIds = company.EmpanelledVendors
                    .Where(vendor => vendor.VendorInvestigationServiceTypes.Any(serviceType =>
                        serviceType.InvestigationServiceTypeId == caseTask.PolicyDetail.InvestigationServiceTypeId &&
                        serviceType.InsuranceType == caseTask.PolicyDetail.InsuranceType &&
                        (serviceType.StateId == pincodeDistrictState.StateId &&
                          (serviceType.SelectedDistrictIds?.Contains(-1) == true
                             || serviceType.SelectedDistrictIds.Contains(pincodeDistrictState.DistrictId.Value)))
                        ))
                    .Select(v => v.VendorId) // Select only VendorId
                    .Distinct() // Ensure uniqueness
                    .ToList();

                if (!distinctVendorIds.Any()) return null; // No vendors found, skip this claim

                // 3. Get Vendor Load & Allocate
                var vendorsWithCases = await _agencyCaseLoadService.GetAgencyIdsLoad(distinctVendorIds);
                var vendorsWithCaseLoad = vendorsWithCases
                    .OrderBy(o => o.CaseCount)
                    .ToList();

                var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
                if (selectedVendorId == null) return null; // No vendors available

                var (policy, status, _) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId.VendorId);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    await AssignToAssigner(userEmail, new List<long> { caseId });
                    await mailboxService.NotifyCaseAssignmentToAssigner(userEmail, new List<long> { caseId }, url);
                    return null;
                }

                // 4. Send Notification
                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAllocationToVendor(userEmail, policy, caseTask.Id, selectedVendorId.VendorId, url));

                return caseTask.PolicyDetail.ContractNumber; // Return allocated claim
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Case {CaseId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }

        public async Task AssignToAssigner(string userEmail, List<long> claims, string url = "")
        {
            try
            {
                if (claims is null || claims.Count == 0)
                {
                    return;
                }
                await using var context = await _contextFactory.CreateDbContextAsync();
                var cases2Assign = context.Investigations
                    .Include(c => c.InvestigationTimeline)
                       .Where(v => claims.Contains(v.Id));
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;

                foreach (var case2Assign in cases2Assign)
                {
                    case2Assign.CaseOwner = currentUser.Email;
                    case2Assign.IsNew = true;
                    case2Assign.Updated = DateTime.UtcNow;
                    case2Assign.UpdatedBy = currentUser.Email;
                    case2Assign.AssignedToAgency = false;
                    case2Assign.IsReady2Assign = case2Assign.IsValidCaseData() ? true : false;
                    case2Assign.VendorId = null;
                    case2Assign.SubStatus = assigned;
                }
                context.Investigations.UpdateRange(cases2Assign);
                await context.SaveChangesAsync(null, false);

                var autoAllocatedTasks = cases2Assign.ToList().Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));

                await Task.WhenAll(autoAllocatedTasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Case(s) {Count}. {UserEmail}", claims.Count, userEmail);
                throw;
            }
        }

        public async Task<(string, string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                // Fetch vendor & user details
                var currentUser = await context.ApplicationUser.AsNoTracking()
                    .Include(c => c.ClientCompany)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                // Fetch case
                var caseTask = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var vendor = await context.Vendor.FindAsync(vendorId);

                // Update case details
                caseTask.IsAutoAllocated = autoAllocated;
                caseTask.IsNew = true;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.AssignedToAgency = true;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.AllocatedToAgencyTime = DateTime.UtcNow;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.AiEnabled = currentUser.ClientCompany.AiEnabled;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                caseTask.Status = CONSTANTS.CASE_STATUS.INPROGRESS;
                caseTask.VendorId = vendorId;
                caseTask.CaseOwner = vendor.Email;
                caseTask.CreatorSla = currentUser.ClientCompany.CreatorSla;
                caseTask.AssessorSla = currentUser.ClientCompany.AssessorSla;
                caseTask.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
                caseTask.AgentSla = currentUser.ClientCompany.AgentSla;
                caseTask.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;

                //REPORT TEMPLATE
                var investigationReport = new InvestigationReport
                {
                    ReportTemplateId = caseTask.ReportTemplateId,
                };

                // Save InvestigationReport
                context.InvestigationReport.Add(investigationReport);

                // Link the InvestigationReport back to the InvestigationTask
                caseTask.InvestigationReport = investigationReport;

                context.Investigations.Update(caseTask);
                // Save changes
                await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return (caseTask.PolicyDetail.ContractNumber, caseTask.SubStatus, vendor.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Case {CasId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }
    }
}