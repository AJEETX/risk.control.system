using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class InvestigationAgencyReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public List<DigitalIdReport>? DigitalIdReports { get; set; } = new();
        public List<DocumentIdReport>? DocumentIdReports { get; set; } = new();

        public long? AgentIdReportId { get; set; }
        public DigitalIdReport? AgentIdReport { get; set; } = new();

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
    }
}
