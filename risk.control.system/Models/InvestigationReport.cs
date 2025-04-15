using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class InvestigationReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? InvestigationAgencyReportId { get; set; }
        public InvestigationAgencyReport? InvestigationAgencyReport { get; set; }

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
