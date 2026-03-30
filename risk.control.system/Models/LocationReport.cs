using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class LocationReport : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [NotMapped]
        public string Address { get; set; } = "Life-Assured";

        public long? ReportTemplateId { get; set; }
        public string? LocationName { get; set; }
        public string? Status { get; set; }

        [EmailAddress]
        public string? AgentEmail { get; set; }

        public AgentIdReport? AgentIdReport { get; set; } = new AgentIdReport
        {
            Selected = true,
            ReportType = DigitalIdReportType.AGENT_FACE,
            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()
        };

        public List<MediaReport>? MediaReports { get; set; } = new();
        public List<FaceIdReport>? FaceIds { get; set; } = new();
        public List<DocumentIdReport>? DocumentIds { get; set; } = new();
        public List<Question>? Questions { get; set; } = new List<Question>();
        public bool IsRequired { get; set; } = false;
        public bool ValidationExecuted { get; set; } = false;

        public override string ToString()
        {
            return $"Location Report: " +
                   $"Location Name={LocationName}, " +
                   $"Status={Status}, " +
                   $"Agent Email={AgentEmail}, " +
                   $"AgentId Report={(AgentIdReport != null ? AgentIdReport.ToString() : "null")}, " +
                   $"MediaReportsCount={(MediaReports != null ? MediaReports.Count : 0)}, " +
                   $"FaceIdsCount={(FaceIds != null ? FaceIds.Count : 0)}, " +
                   $"DocumentIdsCount={(DocumentIds != null ? DocumentIds.Count : 0)}, " +
                   $"QuestionsCount={(Questions != null ? Questions.Count : 0)}, " +
                   $"IsRequired={IsRequired}, " +
                   $"Validation Executed={ValidationExecuted}";
        }

        [NotMapped]
        public long CaseId { get; set; }

        [NotMapped]
        public string LocationStatus = "border-secondary";

        [NotMapped]
        public string LocationStatusButton = "btn-outline-secondary";

        [NotMapped]
        public string AgentStatus = "btn-outline-secondary";

        [NotMapped]
        public string StatusText = "Incomplete";

        [NotMapped]
        public string StatusClass = "bg-light i-red";

        [NotMapped]
        public bool AllQuestionsAnswered => Questions!.Where(q => q.IsRequired).All(q => !string.IsNullOrWhiteSpace(q.AnswerText));

        [NotMapped]
        public bool DocumentsValidated => DocumentIds?.Where(d => d.Selected && d.IsRequired).All(d => d.ImageValid.GetValueOrDefault() && d.IsRequired) ?? false;

        [NotMapped]
        public bool FaceIdsValidated => FaceIds?.Where(f => f.Selected && f.IsRequired).All(f => f.ImageValid.GetValueOrDefault() && f.IsRequired) ?? false;

        [NotMapped]
        public bool AgentValidated => AgentIdReport?.ImageValid ?? false;

        public void SetStatus()
        {
            if (IsRequired)
            {
                if (AllQuestionsAnswered && DocumentsValidated && FaceIdsValidated && AgentValidated)
                {
                    LocationStatusButton = "btn-outline-success";
                    LocationStatus = "border-success";
                    StatusText = "Completed";
                    StatusClass = "bg-light i-green";
                }
                else if (!AllQuestionsAnswered && !DocumentsValidated && !FaceIdsValidated && !AgentValidated)
                {
                    LocationStatusButton = "btn-outline-danger";
                    LocationStatus = "border-danger";
                    StatusText = "Invalid";
                    StatusClass = "bg-light i-red";
                }
                else if (!DocumentsValidated || !FaceIdsValidated || !AgentValidated)
                {
                    LocationStatusButton = "btn-outline-warning";
                    LocationStatus = "border-warning";
                    StatusText = "Partial";
                    StatusClass = "bg-light i-orangered";
                }
                if (AgentValidated)
                {
                    AgentStatus = "btn-outline-success";
                }
                else
                {
                    AgentStatus = "btn-outline-warning";
                }
            }
            else
            {
                if (AllQuestionsAnswered && DocumentsValidated && FaceIdsValidated && AgentValidated)
                {
                    LocationStatusButton = "btn-outline-success";
                    LocationStatus = "border-success";
                    StatusText = "Completed";
                    StatusClass = "bg-light i-green";
                }
                else if (!AllQuestionsAnswered && !DocumentsValidated && !FaceIdsValidated && !AgentValidated)
                {
                    LocationStatusButton = "btn-outline-danger";
                    LocationStatus = "border-danger";
                    StatusText = "Invalid";
                    StatusClass = "bg-light i-red";
                }
                else if (!DocumentsValidated || !FaceIdsValidated || !AgentValidated)
                {
                    LocationStatusButton = "btn-outline-warning";
                    LocationStatus = "border-warning";
                    StatusText = "Partial";
                    StatusClass = "bg-light i-orangered";
                }
                if (AgentValidated)
                {
                    AgentStatus = "btn-outline-success";
                }
                else
                {
                    AgentStatus = "btn-outline-warning";
                }
            }
        }
    }
}
