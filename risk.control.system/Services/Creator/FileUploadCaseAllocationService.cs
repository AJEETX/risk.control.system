using Hangfire;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IFileUploadCaseAllocationService
    {
        Task<List<long>> UploadAutoAllocation(List<InvestigationTask> caseTasks, string userEmail, string url = "");
    }

    internal class FileUploadCaseAllocationService : IFileUploadCaseAllocationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<FileUploadCaseAllocationService> logger;
        private readonly ICaseNotificationService mailboxService;
        private readonly ITimelineService timelineService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public FileUploadCaseAllocationService(IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<FileUploadCaseAllocationService> logger,
            ICaseNotificationService mailboxService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            _contextFactory = contextFactory;
            this.logger = logger;
            this.mailboxService = mailboxService;
            this.timelineService = timelineService;
            this.backgroundJobClient = backgroundJobClient;
        }

        public async Task<List<long>> UploadAutoAllocation(List<InvestigationTask> caseTasks, string userEmail, string url = "")
        {
            var autoAllocatedCases = await DoAutoAllocation(caseTasks, userEmail, url);

            var caseIds = caseTasks.Select(c => c.Id).ToList();

            var notAutoAllocated = caseIds.Except(autoAllocatedCases).ToList();

            if (caseIds.Count > autoAllocatedCases.Count)
            {
                await AssignToAssigner(userEmail, notAutoAllocated, url);
            }
            var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAssignmentToAssigner(userEmail, autoAllocatedCases, notAutoAllocated, url));

            return (autoAllocatedCases);
        }

        private async Task<List<long>> DoAutoAllocation(List<InvestigationTask> caseTasks, string userEmail, string url = "")
        {
            using var initialContext = await _contextFactory.CreateDbContextAsync();
            var companyUser = await initialContext.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

            var company = await initialContext.ClientCompany.AsNoTracking()
                    .Include(c => c.EmpanelledVendors.Where(v => v.Status == VendorStatus.ACTIVE && !v.Deleted))
                    .ThenInclude(e => e.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser!.ClientCompanyId);
            var allVendorIds = company!.EmpanelledVendors.Select(v => v.VendorId)?.ToList();
            if (allVendorIds == null || !allVendorIds.Any()) return new List<long>(); // No vendors, no allocations
            var vendorLoadList = await GetAgencyIdsLoad(allVendorIds);
            var localVendorLoad = vendorLoadList.ToDictionary(v => v.VendorId, v => v.CaseCount);
            var allocatedCaseIds = new List<long>();
            foreach (var caseTask in caseTasks)
            {
                if (caseTask == null || !caseTask.IsValidCaseData()) continue;
                var eligibleVendorIds = company.EmpanelledVendors.Where(vendor => vendor.VendorInvestigationServiceTypes!.Any(st =>
                        st.InvestigationServiceTypeId == caseTask.PolicyDetail!.InvestigationServiceTypeId &&
                        st.InsuranceType == caseTask.PolicyDetail.InsuranceType
                    )).Select(v => v.VendorId)
                    .ToList();
                if (!eligibleVendorIds.Any()) continue;
                var selectedVendorId = eligibleVendorIds.OrderBy(id => localVendorLoad[id]).FirstOrDefault();
                var (policy, status) = await AllocateToVendor(userEmail, caseTask.Id, selectedVendorId);
                if (!string.IsNullOrEmpty(policy))
                {
                    allocatedCaseIds.Add(caseTask.Id);
                    localVendorLoad[selectedVendorId]++;
                    backgroundJobClient.Enqueue(() => mailboxService.NotifyCaseAllocationToVendor(userEmail, policy, caseTask.Id, selectedVendorId, url));
                }
            }
            return allocatedCaseIds;
        }

        private async Task<(string, string)> AllocateToVendor(string userEmail, long caseId, long vendorId, bool autoAllocated = true)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations.Include(c => c.PolicyDetail).Include(c => c.ReportTemplate).ThenInclude(c => c!.LocationReport).ThenInclude(c => c.FaceIds)
                    .Include(c => c.ReportTemplate).ThenInclude(c => c!.LocationReport).ThenInclude(c => c.DocumentIds).Include(c => c.ReportTemplate).ThenInclude(c => c!.LocationReport)
                    .ThenInclude(c => c.Questions).FirstOrDefaultAsync(v => v.Id == caseId);

                var vendor = await context.Vendor.FindAsync(vendorId);
                caseTask!.IsAutoAllocated = autoAllocated;
                caseTask.IsNew = true;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.AssignedToAgency = true;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.AllocatedToAgencyTime = DateTime.UtcNow;
                caseTask.UpdatedBy = currentUser!.Email;
                caseTask.AiEnabled = currentUser.ClientCompany!.AiEnabled;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                caseTask.Status = CONSTANTS.CASE_STATUS.INPROGRESS;
                caseTask.VendorId = vendorId;
                caseTask.CaseOwner = vendor!.Email;
                caseTask.CreatorSla = currentUser.ClientCompany.CreatorSla;
                caseTask.AssessorSla = currentUser.ClientCompany.AssessorSla;
                caseTask.SupervisorSla = currentUser.ClientCompany.SupervisorSla;
                caseTask.AgentSla = currentUser.ClientCompany.AgentSla;
                caseTask.UpdateAgentAnswer = currentUser.ClientCompany.UpdateAgentAnswer;
                var investigationReport = new InvestigationReport
                {
                    ReportTemplateId = caseTask.ReportTemplateId,
                    ReportTemplate = caseTask.ReportTemplate, // Optional
                };
                context.InvestigationReport.Add(investigationReport);
                caseTask.InvestigationReportId = investigationReport.Id;
                caseTask.InvestigationReport = investigationReport;
                context.Investigations.Update(caseTask);
                await context.SaveChangesAsync(null, false);
                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email!);
                return (caseTask.PolicyDetail!.ContractNumber, caseTask.SubStatus);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Case {CasId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }

        private async Task<List<VendorIdWithCases>> GetAgencyIdsLoad(List<long> existingVendors)
        {
            try
            {
                var relevantStatuses = new[] { CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR }; // Improves lookup performance
                await using var context = await _contextFactory.CreateDbContextAsync();
                var vendorCaseCount = context.Investigations.AsNoTracking().Where(c => !c.Deleted && c.VendorId.HasValue && c.AssignedToAgency && relevantStatuses.Contains(c.SubStatus)).GroupBy(c => c.VendorId!.Value).ToDictionary(g => g.Key, g => g.Count());
                return existingVendors.Select(vendorId => new VendorIdWithCases { VendorId = vendorId, CaseCount = vendorCaseCount.GetValueOrDefault(vendorId, 0) }).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred getting Existing Agencies {Count}", existingVendors.Count);
                throw;
            }
        }

        private async Task AssignToAssigner(string userEmail, List<long> caseIds, string url = "")
        {
            try
            {
                if (caseIds is null || caseIds.Count == 0)
                {
                    return;
                }
                await using var context = await _contextFactory.CreateDbContextAsync();
                var cases2Assign = context.Investigations.Include(c => c.PolicyDetail).Include(c => c.CustomerDetail).Include(c => c.BeneficiaryDetail).Include(c => c.InvestigationTimeline).Where(v => caseIds.Contains(v.Id));
                var currentUser = await context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                var assigned = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER;
                foreach (var case2Assign in cases2Assign)
                {
                    case2Assign.CaseOwner = currentUser!.Email;
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
                logger.LogError(ex, "Error occurred Case(s) {Count}. {UserEmail}", caseIds.Count, userEmail);
                throw;
            }
        }
    }
}