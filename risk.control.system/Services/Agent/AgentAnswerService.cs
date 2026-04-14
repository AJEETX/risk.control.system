using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Agent;

public interface IAgentAnswerService
{
    Task<bool> CaptureAnswers(string agentEmail, string locationName, long caseId, List<QuestionTemplate> Questions);
}

internal class AgentAnswerService(ApplicationDbContext context,
    IAgentCaseDetailService caseService,
    ILogger<FaceIdfyService> logger) : IAgentAnswerService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly ILogger<FaceIdfyService> _logger = logger;

    public async Task<bool> CaptureAnswers(string agentEmail, string locationName, long caseId, List<QuestionTemplate> Questions)
    {
        if (string.IsNullOrEmpty(locationName) || caseId <= 0 || Questions == null || Questions.Count == 0)
            return false;
        try
        {
            var caseTask = await _caseService.GetCaseByIdForQuestions(caseId);
            if (caseTask == null)
                return false;
            var location = caseTask.InvestigationReport!.ReportTemplate!.LocationReport.FirstOrDefault(l => l.LocationName == locationName);
            if (location == null)
                return false;
            var locationTemplate = await _context.LocationReport
                .Include(l => l.Questions)
                .FirstOrDefaultAsync(l => l.Id == location.Id);
            if (locationTemplate == null)
                return false;
            locationTemplate.Questions!.RemoveAll(q => true);
            foreach (var q in Questions)
            {
                locationTemplate.Questions.Add(new Question
                {
                    QuestionText = q.QuestionText!,
                    QuestionType = q.QuestionType,
                    IsRequired = q.IsRequired,
                    Options = q.Options?.Trim(),
                    AnswerText = q.AnswerText?.Trim(),
                    Updated = DateTime.UtcNow,
                });
            }
            locationTemplate.ValidationExecuted = true;
            locationTemplate.UpdatedBy = agentEmail;
            locationTemplate.Updated = DateTime.UtcNow;
            _context.LocationReport.Update(locationTemplate);
            var rowsAffected = await _context.SaveChangesAsync(null, false);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            var sanitizedEmail = agentEmail?.Replace("\n", "").Replace("\r", "").Trim();
            _logger.LogError(ex, "Failed capture Answer for {CaseId}. {AgentEmail}", caseId, sanitizedEmail);
            return false;
        }
    }
}