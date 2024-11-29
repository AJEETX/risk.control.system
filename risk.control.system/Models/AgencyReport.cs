using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class AgencyReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AgencyReportId { get; set; }
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public DigitalIdReport? DigitalIdReport { get; set; } = new();

        public ReportQuestionaire? ReportQuestionaire { get; set; } = new();

        public DocumentIdReport? PanIdReport { get; set; } = new();
        public DocumentIdReport? PassportIdReport { get; set; } = new();

        public string? AgentEmail { get; set; }
        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }
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
        public string? ClaimsInvestigationId { get; set; }
        public ClaimsInvestigation? ClaimsInvestigation { get; set; }
        public long? EnquiryRequestId { get; set; }
        public EnquiryRequest? EnquiryRequest { get; set; }
        public List<EnquiryRequest> EnquiryRequests { get; set; } = new List<EnquiryRequest>();
    }
}
