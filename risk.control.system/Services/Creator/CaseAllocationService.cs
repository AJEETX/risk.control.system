using Hangfire;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;

namespace risk.control.system.Services
{
    public interface ICaseAllocationService
    {
        Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> caseIds);

        Task BackgroundAutoAllocation(List<long> claims, string userEmail, string url = "");

        Task<List<long>> BackgroundUploadAutoAllocation(List<long> caseIds, string userEmail, string url = "");

        Task<string> ProcessAutoSingleAllocation(long caseId, string userEmail, string url = "");

        Task<(string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true);
    }

    internal class CaseAllocationService : ICaseAllocationService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<CaseAllocationService> logger;
        private readonly IPdfGenerativeService pdfGenerativeService;
        private readonly IMailService mailboxService;
        private readonly ITimelineService timelineService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public CaseAllocationService(ApplicationDbContext context,
            ILogger<CaseAllocationService> logger,
            IPdfGenerativeService pdfGenerativeService,
            IMailService mailboxService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            this.context = context;
            this.logger = logger;
            this.pdfGenerativeService = pdfGenerativeService;
            this.mailboxService = mailboxService;
            this.timelineService = timelineService;
            this.backgroundJobClient = backgroundJobClient;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<List<long>> BackgroundUploadAutoAllocation(List<long> caseIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(caseIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = caseIds.Except(autoAllocatedCases)?.ToList();

            if (caseIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);
            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));

            return (autoAllocatedCases);
        }

        public async Task<int> UpdateCaseAllocationStatus(string userEmail, List<long> caseIds)
        {
            try
            {
                if (caseIds == null || !caseIds.Any())
                    return 0; // No cases to update

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
                    claimsCase.Updated = DateTime.Now;
                }

                context.Investigations.UpdateRange(cases);
                return await context.SaveChangesAsync(null, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task BackgroundAutoAllocation(List<long> caseIds, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(caseIds, userEmail, url); // Run all tasks in parallel

            var notAutoAllocated = caseIds.Except(autoAllocatedCases)?.ToList();

            if (caseIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);
            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));
        }

        private async Task<List<long>> DoAutoAllocation(List<long> claims, string userEmail, string url = "")
        {
            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var company = await context.ClientCompany
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .ThenInclude(v => v.District)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var claimTasks = claims.Select(async claim =>
            {
                // 1. Fetch Claim Details & Pincode in Parallel
                var caseTask = await context.Investigations
                    .AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                        .ThenInclude(c => c.PinCode)
                    .FirstOrDefaultAsync(c => c.Id == claim);

                if (caseTask == null || !caseTask.IsValidCaseData()) return 0; // Handle missing claim

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

                if (!distinctVendorIds.Any()) return 0; // No vendors found, skip this claim

                // 3. Get Vendor Load & Allocate
                var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
                    .OrderBy(o => o.CaseCount)
                    .ToList();

                var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
                if (selectedVendorId == null) return 0; // No vendors available

                var (policy, status) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId.VendorId);

                if (string.IsNullOrEmpty(policy) || string.IsNullOrEmpty(status))
                {
                    return 0;
                }
                var jobId = backgroundJobClient.Enqueue(() =>
                    mailboxService.NotifyCaseAllocationToVendor(userEmail, policy, caseTask.Id, selectedVendorId.VendorId, url));

                return claim; // Return allocated claim
            });

            var results = await Task.WhenAll(claimTasks); // Run all tasks in parallel
            return results.Where(r => r != null && r != 0).ToList(); // Remove nulls and return allocated claims
        }

        public async Task<string> ProcessAutoSingleAllocation(long caseId, string userEmail, string url = "")
        {
            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var company = await context.ClientCompany
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
            var vendorsWithCaseLoad = GetAgencyIdsLoad(distinctVendorIds)
                .OrderBy(o => o.CaseCount)
                .ToList();

            var selectedVendorId = vendorsWithCaseLoad.FirstOrDefault();
            if (selectedVendorId == null) return null; // No vendors available

            var (policy, status) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId.VendorId);

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

        public async Task AssignToAssigner(string userEmail, List<long> claims, string url = "")
        {
            if (claims is null || claims.Count == 0)
            {
                return;
            }
            var cases2Assign = context.Investigations
                .Include(c => c.InvestigationTimeline)
                   .Where(v => claims.Contains(v.Id));
            var currentUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
            var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;

            foreach (var case2Assign in cases2Assign)
            {
                case2Assign.CaseOwner = currentUser.Email;
                case2Assign.IsNew = true;
                case2Assign.Updated = DateTime.Now;
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

        public async Task<(string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true)
        {
            try
            {
                // Fetch vendor & user details
                var currentUser = await context.ApplicationUser
                    .Include(c => c.ClientCompany)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                // Fetch case
                var caseTask = await context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .ThenInclude(c => c.FaceIds)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .ThenInclude(c => c.DocumentIds)
                    .Include(c => c.ReportTemplate)
                    .ThenInclude(c => c.LocationReport)
                    .ThenInclude(c => c.Questions)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                var vendor = await context.Vendor.FindAsync(vendorId);

                // Update case details
                caseTask.IsAutoAllocated = autoAllocated;
                caseTask.IsNew = true;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.AssignedToAgency = true;
                caseTask.Updated = DateTime.Now;
                caseTask.AllocatedToAgencyTime = DateTime.Now;
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
                    ReportTemplate = caseTask.ReportTemplate, // Optional
                };

                // Save InvestigationReport
                context.InvestigationReport.Add(investigationReport);
                await context.SaveChangesAsync(null, false);

                // Link the InvestigationReport back to the InvestigationTask
                caseTask.InvestigationReportId = investigationReport.Id;
                caseTask.InvestigationReport = investigationReport;

                context.Investigations.Update(caseTask);
                // Save changes
                await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return (caseTask.PolicyDetail.ContractNumber, caseTask.SubStatus);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }

        public List<VendorIdWithCases> GetAgencyIdsLoad(List<long> existingVendors)
        {
            // Get relevant status IDs in one query
            var relevantStatuses = new[]
                {
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                    CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
                }; // Improves lookup performance

            // Fetch cases that match the criteria
            var vendorCaseCount = context.Investigations
                .Where(c => !c.Deleted &&
                            c.VendorId.HasValue &&
                            c.AssignedToAgency &&
                            relevantStatuses.Contains(c.SubStatus))
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
    }
}