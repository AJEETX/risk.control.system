using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface ICloneReportService
    {
        ReportTemplate DeepCloneReportTemplate(ReportTemplate originalTemplate);
    }
    public class CloneReportService : ICloneReportService
    {
        public ReportTemplate DeepCloneReportTemplate(ReportTemplate originalTemplate)
        {
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
                    AgentId = loc.AgentId,
                    Agent = new DigitalIdReport
                    {
                        ReportType = loc.Agent.ReportType,
                        Selected = true,
                        // Copy other DigitalIdReport fields if needed
                    },
                    FaceIds = loc.FaceIds?.Select(face => new DigitalIdReport
                    {
                        ReportType = face.ReportType,
                        Selected = face.Selected,
                        Has2Face = face.Has2Face,
                        IdIName = face.IdIName
                    }).ToList(),

                    DocumentIds = loc.DocumentIds?.Select(doc => new DocumentIdReport
                    {
                        // Copy fields if any
                        DocumentIdReportType = doc.DocumentIdReportType,
                        IdIName = doc.IdIName,
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

    }
}
