using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agency
{
    public interface IAgencyQueryReplyService
    {
        Task<InvestigationTask> SubmitQueryReplyToCompany(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile document);
    }

    internal class AgencyQueryReplyService : IAgencyQueryReplyService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<AgencyQueryReplyService> logger;
        private readonly ITimelineService timelineService;

        public AgencyQueryReplyService(ApplicationDbContext context, ILogger<AgencyQueryReplyService> logger, ITimelineService timelineService)
        {
            this.context = context;
            this.logger = logger;
            this.timelineService = timelineService;
        }

        public async Task<InvestigationTask> SubmitQueryReplyToCompany(string userEmail, long caseId, EnquiryRequest request, List<EnquiryRequest> requests, IFormFile document)
        {
            try
            {
                var caseTask = await context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(p => p.ClientCompany)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequest)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.EnquiryRequests)
                .Include(c => c.Vendor)
                .FirstOrDefaultAsync(c => c.Id == caseId);

                var replyByAgency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
                caseTask.CaseOwner = caseTask.ClientCompany.Email;
                caseTask.SubStatus = replyByAgency;
                caseTask.UpdatedBy = userEmail;
                caseTask.AssignedToAgency = false;
                caseTask.EnquiryReplyByAssessorTime = DateTime.Now;
                caseTask.SubmittedToAssessorTime = DateTime.Now;
                var enquiryRequest = caseTask.InvestigationReport.EnquiryRequest;
                enquiryRequest.DescriptiveAnswer = request.DescriptiveAnswer;

                enquiryRequest.Updated = DateTime.Now;
                enquiryRequest.UpdatedBy = userEmail;

                if (document != null)
                {
                    using var ms = new MemoryStream();
                    document.CopyTo(ms);
                    enquiryRequest.AnswerImageAttachment = ms.ToArray();
                    enquiryRequest.AnswerImageFileName = Path.GetFileName(document.FileName);
                    enquiryRequest.AnswerImageFileExtension = Path.GetExtension(document.FileName);
                    enquiryRequest.AnswerImageFileType = document.ContentType;
                }

                context.QueryRequest.Update(enquiryRequest);
                foreach (var enquiry in requests)
                {
                    var dbEnquiry = caseTask.InvestigationReport.EnquiryRequests
                        .FirstOrDefault(e => e.QueryRequestId == enquiry.QueryRequestId);

                    if (dbEnquiry != null)
                    {
                        dbEnquiry.AnswerSelected = enquiry.AnswerSelected;
                        dbEnquiry.Updated = DateTime.Now;
                        dbEnquiry.UpdatedBy = userEmail;
                    }
                }

                context.Investigations.Update(caseTask);
                var rowsUpdated = await context.SaveChangesAsync(null, false) > 0;
                await timelineService.UpdateTaskStatus(caseTask.Id, userEmail);

                return rowsUpdated ? caseTask : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred Reply Query case {Id}. {UserEmail}", caseId, userEmail);
                return null!;
            }
        }
    }
}