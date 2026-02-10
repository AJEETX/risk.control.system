using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agency
{
    public interface IProcessSubmittedReportService
    {
        Task<InvestigationTask> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseId, SupervisorRemarkType reportUpdateStatus, IFormFile? document = null, string editRemarks = "");

        Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long caseId, string remarks);
    }

    internal class ProcessSubmittedReportService : IProcessSubmittedReportService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<ProcessSubmittedReportService> logger;
        private readonly ITimelineService timelineService;

        public ProcessSubmittedReportService(ApplicationDbContext context, ILogger<ProcessSubmittedReportService> logger, ITimelineService timelineService)
        {
            this.context = context;
            this.logger = logger;
            this.timelineService = timelineService;
        }

        public async Task<InvestigationTask> ProcessAgentReport(string userEmail, string supervisorRemarks, long caseId, SupervisorRemarkType reportUpdateStatus, IFormFile? document = null, string editRemarks = "")
        {
            if (reportUpdateStatus == SupervisorRemarkType.OK)
            {
                return await ApproveAgentReport(userEmail, caseId, supervisorRemarks, reportUpdateStatus, document, editRemarks);
            }
            else
            {
                //PUT th case back in review list :: Assign back to Agent
                return await ReAllocateToVendorAgent(userEmail, caseId, supervisorRemarks, reportUpdateStatus);
            }
        }

        public async Task<(Vendor, string)> SubmitToVendorSupervisor(string userEmail, long caseId, string remarks)
        {
            try
            {
                var agent = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(a => a.Email.Trim().ToLower() == userEmail.Trim().ToLower());

                var submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;

                var caseTask = await GetCases().Include(c => c.InvestigationReport)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = agent.Email;
                caseTask.SubStatus = submitted2Supervisor;
                caseTask.SubmittedToSupervisorTime = DateTime.UtcNow;
                caseTask.CaseOwner = agent.Vendor.Email;
                var claimReport = caseTask.InvestigationReport;

                claimReport.AgentRemarks = remarks;
                claimReport.AgentRemarksUpdated = DateTime.UtcNow;
                claimReport.AgentEmail = userEmail;

                context.Investigations.Update(caseTask);

                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return (agent.Vendor, caseTask.PolicyDetail.ContractNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred submit case {Id}. {UserEmail}", caseId, userEmail);
                return (null, string.Empty);
            }
        }

        private async Task<InvestigationTask> ApproveAgentReport(string userEmail, long caseId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus, IFormFile? document = null, string editRemarks = "")
        {
            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationReport)
                .Include(c => c.Vendor)
                .Include(c => c.ClientCompany)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                var submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
                caseTask.SubmittingSupervisordEmail = userEmail;
                caseTask.SubmittedToAssessorTime = DateTime.UtcNow;
                caseTask.AssignedToAgency = false;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = userEmail;
                caseTask.SubStatus = submitted2Assessor;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                caseTask.SubmittedToAssessorTime = DateTime.UtcNow;
                var report = caseTask.InvestigationReport;
                var edited = report.AgentRemarks.Trim() != editRemarks.Trim();
                if (edited)
                {
                    report.AgentRemarksEdit = editRemarks;
                    report.AgentRemarksEditUpdated = DateTime.UtcNow;
                }

                report.SupervisorRemarkType = reportUpdateStatus;
                report.SupervisorRemarks = supervisorRemarks;
                report.SupervisorRemarksUpdated = DateTime.UtcNow;
                report.SupervisorEmail = userEmail;

                if (document is not null)
                {
                    using var dataStream = new MemoryStream();
                    document.CopyTo(dataStream);
                    report.SupervisorAttachment = dataStream.ToArray();
                    report.SupervisorFileName = Path.GetFileName(document.FileName);
                    report.SupervisorFileExtension = Path.GetExtension(document.FileName);
                    report.SupervisorFileType = document.ContentType;
                }

                context.Investigations.Update(caseTask);
                var rowsAffected = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return rowsAffected ? caseTask : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Approve case {Id}. {UserEmail}", caseId, userEmail);
                return null!;
            }
        }

        private async Task<InvestigationTask> ReAllocateToVendorAgent(string userEmail, long caseId, string supervisorRemarks, SupervisorRemarkType reportUpdateStatus)
        {
            try
            {
                var agencyUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(s => s.Email == userEmail);

                var assignedToAgentSubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
                var caseToAllocateToVendor = await context.Investigations
                    .Include(c => c.InvestigationReport)
                    .Include(c => c.PolicyDetail)
                    .Include(p => p.ClientCompany)
                    .FirstOrDefaultAsync(v => v.Id == caseId);

                //var report = claimsCaseToAllocateToVendor.InvestigationReport;
                //report.SupervisorRemarkType = reportUpdateStatus;
                //report.SupervisorRemarks = supervisorRemarks;
                caseToAllocateToVendor.CaseOwner = agencyUser.Email;
                caseToAllocateToVendor.TaskedAgentEmail = agencyUser.Email;
                caseToAllocateToVendor.Updated = DateTime.UtcNow;
                caseToAllocateToVendor.UpdatedBy = userEmail;
                caseToAllocateToVendor.SubStatus = assignedToAgentSubStatus;
                caseToAllocateToVendor.TaskToAgentTime = DateTime.UtcNow;
                context.Investigations.Update(caseToAllocateToVendor);

                var rowsAffected = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseToAllocateToVendor.Id, userEmail);
                return rowsAffected ? caseToAllocateToVendor : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Re-Allocate case {Id}. {UserEmail}", caseId, userEmail);
                return null!;
            }
        }

        private IQueryable<InvestigationTask> GetCases()
        {
            var caseTasks = context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode);
            return caseTasks;
        }
    }
}