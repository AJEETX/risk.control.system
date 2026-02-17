using Hangfire;

using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;

namespace risk.control.system.Services.Assessor
{
    public interface IProcessCaseService
    {
        Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType reportUpdateStatus, string reportAiSummary);
    }

    internal class ProcessCaseService : IProcessCaseService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<ProcessCaseService> logger;
        private readonly IPdfGenerativeService pdfGenerativeService;
        private readonly ITimelineService timelineService;
        private readonly IBackgroundJobClient backgroundJobClient;

        public ProcessCaseService(ApplicationDbContext context,
            ILogger<ProcessCaseService> logger,
            IPdfGenerativeService pdfGenerativeService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            this.context = context;
            this.logger = logger;
            this.pdfGenerativeService = pdfGenerativeService;
            this.timelineService = timelineService;
            this.backgroundJobClient = backgroundJobClient;
        }

        public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
        {
            if (reportUpdateStatus == AssessorRemarkType.OK)
            {
                return await ApproveCaseReport(userEmail, assessorRemarks, caseId, reportUpdateStatus, reportAiSummary);
            }
            else if (reportUpdateStatus == AssessorRemarkType.REJECT)
            {
                //PUT th case back in review list :: Assign back to Agent
                return await RejectCaseReport(userEmail, assessorRemarks, caseId, reportUpdateStatus, reportAiSummary);
            }
            else
            {
                return (null!, string.Empty);
            }
        }

        private async Task<(ClientCompany, string)> RejectCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            var rejected = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
            var finished = CONSTANTS.CASE_STATUS.FINISHED;

            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.InvestigationReport.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.UtcNow;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = finished;
                caseTask.SubStatus = rejected;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = userEmail;
                caseTask.ProcessedByAssessorTime = DateTime.UtcNow;
                caseTask.SubmittedAssessordEmail = userEmail;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                context.Investigations.Update(caseTask);

                var saveCount = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                backgroundJobClient.Enqueue(() => pdfGenerativeService.Generate(caseId, userEmail));

                var currentUser = await context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                return saveCount > 0 ? (currentUser.ClientCompany, caseTask.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Rejecting Case {CaseId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }

        private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            try
            {
                var approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var finished = CONSTANTS.CASE_STATUS.FINISHED;

                var caseTask = await context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.InvestigationReport.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.UtcNow;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = finished;
                caseTask.SubStatus = approved;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = userEmail;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                caseTask.ProcessedByAssessorTime = DateTime.UtcNow;
                caseTask.SubmittedAssessordEmail = userEmail;
                context.Investigations.Update(caseTask);

                var saveCount = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                backgroundJobClient.Enqueue(() => pdfGenerativeService.Generate(caseId, userEmail));

                return saveCount > 0 ? (caseTask.ClientCompany, caseTask.PolicyDetail.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Approving Case {CaseId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }
    }
}