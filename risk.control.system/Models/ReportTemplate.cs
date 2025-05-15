using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Mvc.Rendering;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class QuestionTemplate
    {
        public string? QuestionText { get; set; }
        public string? QuestionType { get; set; } // "Text", "Radio", "Checkbox"
        public string? Options { get; set; } // comma-separated
        public bool? IsRequired { get; set; }
        public string? Answer { get; set; } // <== This will bind input value
    }


    public class ReportTemplate : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? Name { get; set; }
        public long? ClientCompanyId { get; set; }
        public ClientCompany? ClientCompany { get; set; }
        public InsuranceType InsuranceType { get; set; } = InsuranceType.CLAIM;
        
        public List<LocationTemplate> LocationTemplate { get; set; } = new List<LocationTemplate>();
        public bool Basetemplate { get; set; } = false;
        public long? OriginalTemplateId { get; set; }
        [NotMapped]
        public long CaseId { get; set; }
    }

    public class LocationTemplate : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
        public string? LocationName { get; set; }
        public string? Status { get; set; }
        public string? AgentEmail { get; set; }
        public long? AgentIdReportId { get; set; }
        public AgentIdReport? AgentIdReport { get; set; } = new AgentIdReport 
        { 
            Selected = true, 
            ReportType = DigitalIdReportType.AGENT_FACE,
            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()
        };

        public List<DigitalIdReport>? FaceIds { get; set; } = new();
        public List<DocumentIdReport>? DocumentIds { get; set; } = new();
        public List<Question>? Questions { get; set; } = new List<Question>();
        public bool IsRequired { get; set; } = false;
        public bool ValidationExecuted { get; set; } = false;

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
        public string StatusClass = "bg-danger text-white";
        [NotMapped]
        public bool AllQuestionsAnswered => Questions.Where(q => q.IsRequired).All(q => !string.IsNullOrWhiteSpace(q.AnswerText));


        [NotMapped]
        public bool DocumentsValidated => DocumentIds?.Where(d => d.Selected && d.IsRequired).All(d => d.IdImageValid.GetValueOrDefault() && d.IsRequired) ?? false;
        [NotMapped]
        public bool FaceIdsValidated => FaceIds?.Where(f => f.Selected && f.IsRequired).All(f => f.IdImageValid.GetValueOrDefault() && f.IsRequired) ?? false;
        [NotMapped]
        public bool AgentValidated => AgentIdReport?.IdImageValid ?? false;

        public void SetStatus()
        {
            if (IsRequired && AllQuestionsAnswered && DocumentsValidated && FaceIdsValidated && AgentValidated)
                {
                    LocationStatusButton = "btn-outline-success";
                    LocationStatus = "border-success";
                    StatusText = "Completed";
                    StatusClass =  "bg-success text-white";
                }
            else if (!IsRequired && !AllQuestionsAnswered && !DocumentsValidated && !FaceIdsValidated && !AgentValidated)
            {
                LocationStatusButton = "btn-outline-danger";
                LocationStatus = "border-danger";
                StatusText = "Invalid";
                StatusClass = "bg-danger text-white";
            }
            else if (IsRequired && (AllQuestionsAnswered || DocumentsValidated || FaceIdsValidated || AgentValidated))
            {
                LocationStatusButton = "btn-outline-warning";
                LocationStatus = "border-warning";
                StatusText = "Partial";
                StatusClass = "bg-warning text-dark";
            }
            if(IsRequired && AgentValidated)
            {
                AgentStatus = "btn-outline-success";
            }
            else
            {
                AgentStatus = "btn-outline-warning";
            }
        }
    }
    public class ReportTemplateCreateViewModel
    {
        public string? Name { get; set; }
        public InsuranceType InsuranceType { get; set; } = InsuranceType.CLAIM;
        public long? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
        public List<FaceIdCreateViewModel>? FaceIds { get; set; } = new();
        public List<DocumentIdCreateViewModel>? DocumentIds { get; set; } = new();
        public QuestionFormViewModel? Questions { get; set; } = new();
        public List<SelectListItem>? DigitalIdReportItems { get; set; }
        public List<SelectListItem>? DocumentIdReportItems { get; set; }
    }

    public class FaceIdCreateViewModel
    {
        public long? Id { get; set; }
        public DigitalIdReportType? DigitalIdReportType { get; set; }
        public string? FaceIdName { get; set; }
    }

    public class DocumentIdCreateViewModel
    {
        public long? Id { get; set; }
        public DocumentIdReportType? DocumentIdReportType { get; set; }
        public string? DocumentIdName { get; set; }
    }

}
