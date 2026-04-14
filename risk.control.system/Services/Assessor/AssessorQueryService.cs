using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Assessor
{
    public interface IAssessorQueryService
    {
        Task<InvestigationTask> SubmitQueryToAgency(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile? document);
    }

    public class AssessorQueryService(ApplicationDbContext context, ILogger<AssessorQueryService> logger, ITimelineService timelineService) : IAssessorQueryService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AssessorQueryService> _logger = logger;
        private readonly ITimelineService _timelineService = timelineService;

        public async Task<InvestigationTask> SubmitQueryToAgency(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile? document)
        {
            try
            {
                var caseTask = await _context.Investigations
                .Include(c => c.Vendor)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c!.EnquiryRequest)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c!.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask!.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
                caseTask.UpdatedBy = userEmail;
                caseTask.CaseOwner = caseTask.Vendor!.Email;
                caseTask.RequestedAssessordEmail = userEmail;
                caseTask.AssignedToAgency = true;
                caseTask.IsQueryCase = true;
                if (document != null)
                {
                    await using var ms = new MemoryStream();
                    document.CopyTo(ms);
                    request.QuestionImageAttachment = ms.ToArray();
                    request.QuestionImageFileName = Path.GetFileName(document.FileName);
                    request.QuestionImageFileExtension = Path.GetExtension(document.FileName);
                    request.QuestionImageFileType = document.ContentType;
                }
                caseTask.InvestigationReport!.EnquiryRequest = request;
                caseTask.InvestigationReport.EnquiryRequests = requests;
                caseTask.InvestigationReport.Updated = DateTime.UtcNow;
                caseTask.InvestigationReport.UpdatedBy = userEmail;
                caseTask.InvestigationReport.EnquiryRequest.Updated = DateTime.UtcNow;
                caseTask.InvestigationReport.EnquiryRequest.UpdatedBy = userEmail;
                caseTask.EnquiredByAssessorTime = DateTime.UtcNow;
                _context.QueryRequest.Update(request);
                _context.Investigations.Update(caseTask);

                var saved = await _context.SaveChangesAsync(null, false) > 0;

                await _timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return saved ? caseTask : null!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred Submit Query case {Id}. {UserEmail}", caseId, userEmail);
                return null!;
            }
        }
    }
}