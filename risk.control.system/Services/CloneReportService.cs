using System.Text.Json;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface ICloneReportService
    {
        Task<ReportTemplate> DeepCloneReportTemplate(long clientCompanyId, InsuranceType insuranceType);
        Task<object> GetReportTemplate(long caseId, string agentEmail);
    }
    public class CloneReportService : ICloneReportService
    {
        private readonly ApplicationDbContext context;

        public CloneReportService(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<ReportTemplate> DeepCloneReportTemplate(long clientCompanyId, InsuranceType insuranceType)
        {
            var originalTemplate = await context.ReportTemplates
                .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.AgentIdReport)
                   .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.Questions)
            .FirstOrDefaultAsync(r => r.ClientCompanyId == clientCompanyId && r.InsuranceType == insuranceType && r.Basetemplate);
            var clone = new ReportTemplate
            {
                Name = originalTemplate.Name,
                ClientCompanyId = originalTemplate.ClientCompanyId,
                InsuranceType = originalTemplate.InsuranceType,
                Basetemplate = false, // Set to false for the cloned template
                OriginalTemplateId = originalTemplate.Id, // Reference to the original template
                Created = DateTime.UtcNow,
                UpdatedBy = "system", // Or current user
                LocationTemplate = originalTemplate.LocationTemplate.Select(loc => new LocationTemplate
                {
                    LocationName = loc.LocationName,
                    IsRequired = loc.IsRequired,
                    AgentIdReport = new AgentIdReport
                    {
                        IsRequired = loc.AgentIdReport.IsRequired,
                        ReportType = loc.AgentIdReport.ReportType,
                        ReportName = loc.AgentIdReport.ReportName,
                    },
                    FaceIds = loc.FaceIds?.Select(face => new DigitalIdReport
                    {
                        IsRequired = face.IsRequired,
                        ReportType = face.ReportType,
                        Selected = face.Selected,
                        Has2Face = face.Has2Face,
                        ReportName = face.ReportName
                    }).ToList(),

                    DocumentIds = loc.DocumentIds?.Select(doc => new DocumentIdReport
                    {
                        IsRequired = doc.IsRequired,
                        ReportType = doc.ReportType,
                        ReportName = doc.ReportName,
                        IdImageBack = doc.IdImageBack,
                        Selected = doc.Selected,
                    }).ToList(),

                    Questions = loc.Questions?.Select(q => new Question
                    {
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Options = q.Options,
                        IsRequired = q.IsRequired,
                        // Copy other fields
                    }).ToList()
                }).ToList()
            };

            return clone;
        }
        public async Task<object> GetReportTemplate(long caseId, string agentEmail)
        {
            var investigation = await context.Investigations.FindAsync(caseId);

            var originalTemplate = await context.ReportTemplates
                 .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.Questions)
            .FirstOrDefaultAsync(r => r.Id == investigation.ReportTemplateId);

            var locationTemplate = originalTemplate.LocationTemplate.Select(loc => new             
            {
                LocationName = loc.LocationName,
                IsRequired = loc.IsRequired,
                Agent = new 
                {
                    IsRequired = loc.AgentIdReport.IsRequired,
                    ReportType = loc.AgentIdReport.ReportType.GetEnumDisplayName(),
                    ReportName = loc.AgentIdReport.ReportName
                },
                FaceIds = loc.FaceIds.Where(face => face.Selected)?.Select(face => new 
                {
                    IsRequired = face.IsRequired,
                    ReportType = face.ReportType.GetEnumDisplayName(),
                    Has2Face = face.Has2Face,
                    ReportName = face.ReportName
                }).ToList(),

                DocumentIds = loc.DocumentIds.Where(face => face.Selected)?.Select(doc => new 
                {
                    IsRequired = doc.IsRequired,
                    ReportType = doc.ReportType.GetEnumDisplayName(),
                    ReportName = doc.ReportName,
                    IdImageBack = doc.IdImageBack,
                }).ToList(),

                Questions = loc.Questions?.Select(q => new 
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Options = q.Options,
                    IsRequired = q.IsRequired,
                    AnswerText = q.AnswerText
                }).ToList()
            }).ToList();

            return locationTemplate;
        }
        
    }
}
