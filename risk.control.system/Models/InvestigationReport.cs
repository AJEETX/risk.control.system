using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class InvestigationReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
        public long? DigitalIdReportId { get; set; }
        public DigitalIdReport? DigitalIdReport { get; set; } = new();

        public long? AgentIdReportId { get; set; }
        public DigitalIdReport? AgentIdReport { get; set; } = new();

        public long? PanIdReportId { get; set; } = new();
        public DocumentIdReport? PanIdReport { get; set; } = new();
        public long CaseQuestionnaireId { get; set; }
        public CaseQuestionnaire CaseQuestionnaire { get; set; } = new();

        public string? AgentEmail { get; set; }
        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }

        public DateTime? AgentRemarksEditUpdated { get; set; }
        public string? AgentRemarksEdit { get; set; }
        public DateTime? SupervisorRemarksUpdated { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorRemarks { get; set; }
        public byte[]? SupervisorAttachment { get; set; }
        public string? SupervisorFileName { get; set; }
        public string? SupervisorFileType { get; set; }
        public string? SupervisorFileExtension { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }

        public DateTime? AssessorRemarksUpdated { get; set; }
        public string? AssessorEmail { get; set; }
        public string? AssessorRemarks { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }
        public long? EnquiryRequestId { get; set; }
        public EnquiryRequest? EnquiryRequest { get; set; }
        public List<EnquiryRequest> EnquiryRequests { get; set; } = new List<EnquiryRequest>();

        public string? AiSummary { get; set; }
        public DateTime? AiSummaryUpdated { get; set; }

        public string? PdfReportFilePath { get; set; }

    }
}
