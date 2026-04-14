using System.Net;
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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProcessCaseService> _logger;
        private readonly IPdfGenerativeService _pdfGenerativeService;
        private readonly ITimelineService _timelineService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        public ProcessCaseService(ApplicationDbContext context,
            ILogger<ProcessCaseService> logger,
            IPdfGenerativeService pdfGenerativeService,
            ITimelineService timelineService,
            IBackgroundJobClient backgroundJobClient)
        {
            this._context = context;
            this._logger = logger;
            this._pdfGenerativeService = pdfGenerativeService;
            this._timelineService = timelineService;
            this._backgroundJobClient = backgroundJobClient;
        }

        public async Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType reportUpdateStatus, string reportAiSummary)
        {
            assessorRemarks = WebUtility.HtmlEncode(assessorRemarks);
            reportAiSummary = WebUtility.HtmlEncode(reportAiSummary);

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
                var caseTask = await _context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask!.InvestigationReport!.AiSummary = reportAiSummary;
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
                caseTask.CaseOwner = caseTask.ClientCompany!.Email;
                _context.Investigations.Update(caseTask);

                var saveCount = await _context.SaveChangesAsync(null, false);

                await _timelineService.UpdateTaskStatus(caseTask.Id, userEmail);
                _backgroundJobClient.Enqueue(() => _pdfGenerativeService.Generate(caseId, userEmail));

                var currentUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == userEmail);
                return saveCount > 0 ? (currentUser!.ClientCompany!, caseTask.PolicyDetail!.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred Rejecting Case {CaseId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }

        private async Task<(ClientCompany, string)> ApproveCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType assessorRemarkType, string reportAiSummary)
        {
            try
            {
                var approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var finished = CONSTANTS.CASE_STATUS.FINISHED;

                var caseTask = await _context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask!.InvestigationReport!.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.UtcNow;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = finished;
                caseTask.SubStatus = approved;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = userEmail;
                caseTask.CaseOwner = caseTask.ClientCompany!.Email;
                caseTask.ProcessedByAssessorTime = DateTime.UtcNow;
                caseTask.SubmittedAssessordEmail = userEmail;
                _context.Investigations.Update(caseTask);

                var saveCount = await _context.SaveChangesAsync(null, false);

                await _timelineService.UpdateTaskStatus(caseTask.Id, userEmail);
                _backgroundJobClient.Enqueue(() => _pdfGenerativeService.Generate(caseId, userEmail));

                return saveCount > 0 ? (caseTask.ClientCompany, caseTask.PolicyDetail!.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred Approving Case {CaseId}. {UserEmail}", caseId, userEmail);
                throw;
            }
        }
    }
}