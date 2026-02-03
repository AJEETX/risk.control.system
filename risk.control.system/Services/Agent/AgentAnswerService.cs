using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Agent;

public interface IAgentAnswerService
{
    Task<bool> CaptureAnswers(string agentEmail, string locationName, long caseId, List<QuestionTemplate> Questions);
}

internal class AgentAnswerService : IAgentAnswerService
{
    private readonly ApplicationDbContext context;
    private readonly IAgentCaseDetailService caseService;
    private readonly ILogger<FaceIdfyService> logger;

    public AgentAnswerService(ApplicationDbContext context,
        IAgentCaseDetailService caseService,
        ILogger<FaceIdfyService> logger)
    {
        this.context = context;
        this.caseService = caseService;
        this.logger = logger;
    }

    public async Task<bool> CaptureAnswers(string agentEmail, string locationName, long caseId, List<QuestionTemplate> Questions)
    {
        if (string.IsNullOrEmpty(locationName) || caseId <= 0 || Questions == null || Questions.Count == 0)
            return false;
        try
        {
            var caseTask = await caseService.GetCaseByIdForQuestions(caseId);
            if (caseTask == null)
                return false;
            var location = caseTask.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == locationName);
            if (location == null)
                return false;
            var locationTemplate = await context.LocationReport
                .Include(l => l.Questions)
                .FirstOrDefaultAsync(l => l.Id == location.Id);
            if (locationTemplate == null)
                return false;
            locationTemplate.Questions.RemoveAll(q => true);
            foreach (var q in Questions)
            {
                locationTemplate.Questions.Add(new Question
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    IsRequired = q.IsRequired,
                    Options = q.Options?.Trim(),
                    AnswerText = q.AnswerText?.Trim(),
                    Updated = DateTime.Now,
                });
            }
            locationTemplate.ValidationExecuted = true;
            locationTemplate.UpdatedBy = agentEmail;
            locationTemplate.Updated = DateTime.Now;
            context.LocationReport.Update(locationTemplate);
            var rowsAffected = await context.SaveChangesAsync(null, false);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed capture Answer for {CaseId}. {AgentEmail}", caseId, agentEmail);
            return false;
        }
    }
}