using Microsoft.EntityFrameworkCore;

using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services;

public interface IAnswerService
{
    Task<bool> CaptureAnswers(string locationName, long caseId, List<QuestionTemplate> Questions);
}

internal class AnswerService : IAnswerService
{
    private readonly ApplicationDbContext context;
    private readonly ICaseService caseService;
    private readonly ILogger<AgentIdfyService> logger;

    public AnswerService(ApplicationDbContext context,
        ICaseService caseService,
        ILogger<AgentIdfyService> logger)
    {
        this.context = context;
        this.caseService = caseService;
        this.logger = logger;
    }

    public async Task<bool> CaptureAnswers(string locationName, long caseId, List<QuestionTemplate> Questions)
    {
        try
        {
            var claim = await caseService.GetCaseByIdForQuestions(caseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == locationName);

            var locationTemplate = await context.LocationReport
                .Include(l => l.Questions)
                .FirstOrDefaultAsync(l => l.Id == location.Id);

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
            locationTemplate.Updated = DateTime.Now;
            context.LocationReport.Update(locationTemplate);
            var rowsAffected = await context.SaveChangesAsync(null, false);
            return rowsAffected > 0;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed media file capture");
            return false;
        }
    }
}