using System.Net;
using Hangfire;

using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;

namespace risk.control.system.Services.Assessor
{
    public interface IProcessCaseService
    {
        Task<(ClientCompany, string)> ProcessCaseReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType reportUpdateStatus, string reportAiSummary);
        Task<bool> SubmitCaseReportAsync(SubmitCaseRequest request);
    }

    internal class ProcessCaseService : IProcessCaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProcessCaseService> _logger;
        private readonly IPdfReportService _pdfGenerativeService;
        private readonly ITimelineService _timelineService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        public ProcessCaseService(ApplicationDbContext context,
            ILogger<ProcessCaseService> logger,
            IPdfReportService pdfGenerativeService,
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
                string approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                return await ProcessReport(userEmail, assessorRemarks, caseId, reportUpdateStatus, approved, reportAiSummary);
            }
            else if (reportUpdateStatus == AssessorRemarkType.REJECT)
            {
                string rejected = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
                return await ProcessReport(userEmail, assessorRemarks, caseId, reportUpdateStatus, rejected, reportAiSummary);
            }
            else
            {
                return (null!, string.Empty);
            }
        }


        public async Task<bool> SubmitCaseReportAsync(SubmitCaseRequest request)
        {
            var remarkType = Enum.Parse<AssessorRemarkType>(request.AssessorRemarkType);
            if (remarkType == AssessorRemarkType.OK)
            {
                string approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
                var result = await ProcessCaseAIReport(request.Email, request.AssessorRemarks, request.PolicyNumber, remarkType, approved, string.Empty);
                if (result.Item1 != null)
                {
                    return true;
                }
            }
            else if (remarkType == AssessorRemarkType.REJECT)
            {
                string rejected = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

                var result = await ProcessCaseAIReport(request.Email, request.AssessorRemarks, request.PolicyNumber, remarkType, rejected, string.Empty);
                if (result.Item1 != null)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<(ClientCompany, string)> ProcessCaseAIReport(string userEmail, string assessorRemarks, string contractNumber, AssessorRemarkType assessorRemarkType, string processed, string reportAiSummary)
        {
            try
            {
                var finished = CONSTANTS.CASE_STATUS.FINISHED;

                var caseTask = await _context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => !c.Deleted && c.PolicyDetail!.ContractNumber == contractNumber);

                caseTask!.InvestigationReport!.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.UtcNow;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = finished;
                caseTask.SubStatus = processed;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = userEmail;
                caseTask.CaseOwner = caseTask.ClientCompany!.Email;
                caseTask.ProcessedByAssessorTime = DateTime.UtcNow;
                caseTask.SubmittedAssessordEmail = userEmail;
                _context.Investigations.Update(caseTask);

                var saveCount = await _context.SaveChangesAsync(null, false);

                await _timelineService.UpdateTaskStatus(caseTask.Id, userEmail);
                _backgroundJobClient.Enqueue(() => _pdfGenerativeService.Generate(caseTask.Id, userEmail));

                return saveCount > 0 ? (caseTask.ClientCompany, caseTask.PolicyDetail!.ContractNumber) : (null!, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred Processing Case {contractNumber}. {UserEmail}", contractNumber, userEmail);
                throw;
            }
        }

        private async Task<(ClientCompany, string)> ProcessReport(string userEmail, string assessorRemarks, long caseId, AssessorRemarkType assessorRemarkType, string subStatus, string reportAiSummary)
        {
            try
            {
                var caseTask = await _context.Investigations
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .Include(r => r.InvestigationReport)
                .FirstOrDefaultAsync(c => !c.Deleted && c.Id == caseId);

                caseTask!.InvestigationReport!.AiSummary = reportAiSummary;
                caseTask.InvestigationReport.AssessorRemarkType = assessorRemarkType;
                caseTask.InvestigationReport.AssessorRemarks = assessorRemarks;
                caseTask.InvestigationReport.AssessorRemarksUpdated = DateTime.UtcNow;
                caseTask.InvestigationReport.AssessorEmail = userEmail;

                caseTask.Status = CONSTANTS.CASE_STATUS.FINISHED;
                caseTask.SubStatus = subStatus;
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