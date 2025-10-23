using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
namespace risk.control.system.Services
{
    public interface ICloneReportService
    {
        Task<ReportTemplate> DeepCloneReportTemplate(long clientCompanyId, InsuranceType insuranceType);
        Task<ReportTemplate> CreateCloneReportTemplate(long templateId, string currentUserEmail);
        Task<bool> Activate(long templateId);
        Task<object> GetReportTemplate(long caseId, string agentEmail);
    }
    public class CloneReportService : ICloneReportService
    {
        private readonly ApplicationDbContext context;

        public CloneReportService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> Activate(long templateId)
        {
            var template = await context.ReportTemplates
                .FirstOrDefaultAsync(r => r.Id == templateId);

            if (template == null)
                return false;

            // Deactivate all other templates of same InsuranceType
            var sameTypeTemplates = await context.ReportTemplates
                .Where(r => r.InsuranceType == template.InsuranceType && r.Id != templateId)
                .ToListAsync();

            foreach (var t in sameTypeTemplates)
            {
                t.IsActive = false;
            }

            // Activate selected template
            template.IsActive = true;

            // Update entities
            context.ReportTemplates.UpdateRange(sameTypeTemplates);
            context.ReportTemplates.Update(template);

            var rowsAffected = await context.SaveChangesAsync();
            return rowsAffected > 0;
        }

        public async Task<ReportTemplate> CreateCloneReportTemplate(long templateId, string currentUserEmail)
        {
            var originalTemplate = await context.ReportTemplates
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.AgentIdReport)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.FaceIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.DocumentIds)
                .Include(r => r.LocationReport)
                    .ThenInclude(l => l.Questions)
                .FirstOrDefaultAsync(r => r.Id == templateId);

            string baseName = Regex.Replace(originalTemplate.Name, @"_\d{8}_\d{6,9}$", "");
            string newName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}";

            var clone = new ReportTemplate
            {
                Name = newName,
                ClientCompanyId = originalTemplate.ClientCompanyId,
                InsuranceType = originalTemplate.InsuranceType,
                Basetemplate = false, // Set to false for the cloned template
                OriginalTemplateId = originalTemplate.Id, // Reference to the original template
                Created = DateTime.Now,
                UpdatedBy = currentUserEmail, // Or current user
                LocationReport = originalTemplate.LocationReport.Select(loc => new LocationReport
                {
                    LocationName = loc.LocationName,
                    IsRequired = loc.IsRequired,
                    AgentIdReport = new AgentIdReport
                    {
                        Selected = loc.AgentIdReport.Selected,
                        IsRequired = loc.AgentIdReport.IsRequired,
                        ReportType = loc.AgentIdReport.ReportType,
                        ReportName = loc.AgentIdReport.ReportName,
                    },
                    MediaReports = loc.MediaReports?.Select(m => new MediaReport
                    {
                        IsRequired = m.IsRequired,
                        ReportName = m.ReportName,
                        MediaType = m.MediaType,
                        Selected = m.Selected,
                    }).ToList(),
                    FaceIds = loc.FaceIds?.Select(face => new FaceIdReport
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

            var addedTemplate = await context.ReportTemplates.AddAsync(clone);
            var rowsAffected = await context.SaveChangesAsync();
            return addedTemplate.Entity;
        }

        public async Task<ReportTemplate> DeepCloneReportTemplate(long clientCompanyId, InsuranceType insuranceType)
        {
            var originalTemplate = await context.ReportTemplates
                .Include(r => r.LocationReport)
                   .ThenInclude(l => l.AgentIdReport)
                    .Include(r => r.LocationReport)
                   .ThenInclude(l => l.MediaReports)
                   .Include(r => r.LocationReport)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.Questions)
            .FirstOrDefaultAsync(r => r.ClientCompanyId == clientCompanyId && r.InsuranceType == insuranceType && r.IsActive);

            var clone = new ReportTemplate
            {
                Name = originalTemplate.Name,
                ClientCompanyId = originalTemplate.ClientCompanyId,
                InsuranceType = originalTemplate.InsuranceType,
                Basetemplate = false, // Set to false for the cloned template
                OriginalTemplateId = originalTemplate.Id, // Reference to the original template
                Created = DateTime.Now,
                UpdatedBy = "system", // Or current user
                LocationReport = originalTemplate.LocationReport.Select(loc => new LocationReport
                {
                    LocationName = loc.LocationName,
                    IsRequired = loc.IsRequired,
                    AgentIdReport = new AgentIdReport
                    {
                        Selected = loc.AgentIdReport.Selected,
                        IsRequired = loc.AgentIdReport.IsRequired,
                        ReportType = loc.AgentIdReport.ReportType,
                        ReportName = loc.AgentIdReport.ReportName,
                    },
                    MediaReports = loc.MediaReports?.Select(m => new MediaReport
                    {
                        IsRequired = m.IsRequired,
                        ReportName = m.ReportName,
                        MediaType = m.MediaType,
                        Selected = m.Selected,
                    }).ToList(),
                    FaceIds = loc.FaceIds?.Select(face => new FaceIdReport
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
                 .Include(r => r.LocationReport)
                   .ThenInclude(l => l.AgentIdReport)
                   .Include(r => r.LocationReport)
                   .ThenInclude(l => l.MediaReports)
                .Include(r => r.LocationReport)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.Questions)
            .FirstOrDefaultAsync(r => r.Id == investigation.ReportTemplateId);

            var locationTemplate = originalTemplate.LocationReport.Select(loc => new
            {
                LocationName = loc.LocationName,
                IsRequired = loc.IsRequired,
                Agent = new
                {
                    IsRequired = loc.AgentIdReport.IsRequired,
                    ReportType = loc.AgentIdReport.ReportType.GetEnumDisplayName(),
                    ReportName = loc.AgentIdReport.ReportName
                },
                MediaReports = loc.MediaReports?.Select(m => new MediaReport
                {
                    IsRequired = m.IsRequired,
                    ReportName = m.ReportName,
                    MediaType = m.MediaType,
                    Selected = m.Selected,
                    MediaExtension = m.MediaExtension
                }).ToList(),
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
