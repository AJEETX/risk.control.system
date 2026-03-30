using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface ICloneReportService
    {
        Task<ReportTemplate> DeepCloneReportTemplate(long clientCompanyId, InsuranceType insuranceType);

        Task<ReportTemplate> CreateCloneReportTemplate(long templateId, string currentUserEmail);

        Task<bool> Activate(long templateId);

        Task<object> GetReportTemplate(long caseId, string agentEmail);
    }

    internal class CloneReportService : ICloneReportService
    {
        private readonly ApplicationDbContext context;

        public CloneReportService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> Activate(long templateId)
        {
            var template = await context.ReportTemplates.FirstOrDefaultAsync(r => r.Id == templateId);
            if (template == null)
                return false;
            var sameTypeTemplates = await context.ReportTemplates.Where(r => r.InsuranceType == template.InsuranceType && r.Id != templateId).ToListAsync();
            foreach (var t in sameTypeTemplates)
            {
                t.IsActive = false;
            }
            template.IsActive = true;
            context.ReportTemplates.UpdateRange(sameTypeTemplates);
            context.ReportTemplates.Update(template);
            var rowsAffected = await context.SaveChangesAsync(null, false);
            return rowsAffected > 0;
        }

        public async Task<ReportTemplate> CreateCloneReportTemplate(long templateId, string currentUserEmail)
        {
            var originalTemplate = await GetReportTemplateDetails(templateId);
            string newName = $"{Regex.Replace(originalTemplate!.Name!, @"_\d{8}_\d{6,9}$", "")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var clone = CloneReportTemplate(newName, originalTemplate, currentUserEmail);
            var addedTemplate = await context.ReportTemplates.AddAsync(clone);
            var rowsAffected = await context.SaveChangesAsync(null, false);
            return addedTemplate.Entity;
        }

        public async Task<ReportTemplate> DeepCloneReportTemplate(long clientCompanyId, InsuranceType insuranceType)
        {
            var originalTemplate = await GetReportDefaultTemplateDetails(clientCompanyId, insuranceType);
            var clone = DeepCloneReportTemplate(originalTemplate!);
            return clone;
        }

        public async Task<object> GetReportTemplate(long caseId, string agentEmail)
        {
            var investigation = await context.Investigations.FindAsync(caseId);
            var originalTemplate = await GetReportTemplateDetails(investigation!.ReportTemplateId!);
            var locationTemplate = originalTemplate!.LocationReport.Select(loc => new
            {
                LocationName = loc.LocationName,
                IsRequired = loc.IsRequired,
                Agent = new
                {
                    IsRequired = loc.AgentIdReport!.IsRequired,
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
                FaceIds = loc.FaceIds!.Where(face => face.Selected)?.Select(face => new
                {
                    IsRequired = face.IsRequired,
                    ReportType = face.ReportType.GetEnumDisplayName(),
                    Has2Face = face.Has2Face,
                    ReportName = face.ReportName
                }).ToList(),

                DocumentIds = loc.DocumentIds!.Where(face => face.Selected)?.Select(doc => new
                {
                    IsRequired = doc.IsRequired,
                    ReportType = doc.ReportType.GetEnumDisplayName(),
                    ReportName = doc.ReportName,
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
        private ReportTemplate DeepCloneReportTemplate(ReportTemplate originalTemplate)
        {
            return new ReportTemplate
            {
                Name = originalTemplate!.Name,
                ClientCompanyId = originalTemplate.ClientCompanyId,
                InsuranceType = originalTemplate.InsuranceType,
                Basetemplate = false, // Set to false for the cloned template
                OriginalTemplateId = originalTemplate.Id, // Reference to the original template
                Created = DateTime.UtcNow,
                UpdatedBy = "system", // Or current user
                LocationReport = originalTemplate.LocationReport.Select(loc => new LocationReport
                {
                    LocationName = loc.LocationName,
                    IsRequired = loc.IsRequired,
                    AgentIdReport = new AgentIdReport
                    {
                        Selected = loc.AgentIdReport!.Selected,
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
        }
        private ReportTemplate CloneReportTemplate(string newName, ReportTemplate originalTemplate, string currentUserEmail)
        {
            return new ReportTemplate
            {
                Name = newName,
                ClientCompanyId = originalTemplate.ClientCompanyId,
                InsuranceType = originalTemplate.InsuranceType,
                Basetemplate = false, // Set to false for the cloned template
                OriginalTemplateId = originalTemplate.Id, // Reference to the original template
                Created = DateTime.UtcNow,
                UpdatedBy = currentUserEmail, // Or current user
                LocationReport = originalTemplate.LocationReport.Select(loc => new LocationReport
                {
                    LocationName = loc.LocationName,
                    IsRequired = loc.IsRequired,
                    AgentIdReport = new AgentIdReport
                    {
                        Selected = loc.AgentIdReport!.Selected,
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
                        Selected = doc.Selected,
                    }).ToList(),

                    Questions = loc.Questions?.Select(q => new Question
                    {
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Options = q.Options,
                        IsRequired = q.IsRequired,
                    }).ToList()
                }).ToList()
            };
        }
        private async Task<ReportTemplate?> GetReportTemplateDetails(long? id)
        {
            return await context.ReportTemplates
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
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        private async Task<ReportTemplate?> GetReportDefaultTemplateDetails(long? clientCompanyId, InsuranceType insuranceType)
        {
            return await context.ReportTemplates
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
        }
    }
}