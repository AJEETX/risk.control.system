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
        [EmailAddress]
        public string? AgentEmail { get; set; }
        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }
        public DateTime? AgentRemarksEditUpdated { get; set; }
        public string? AgentRemarksEdit { get; set; }
        public DateTime? SupervisorRemarksUpdated { get; set; }
        [EmailAddress]
        public string? SupervisorEmail { get; set; }
        public string? SupervisorRemarks { get; set; }
        public byte[]? SupervisorAttachment { get; set; }
        public string? SupervisorFileName { get; set; }
        public string? SupervisorFileType { get; set; }
        public string? SupervisorFileExtension { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }
        public DateTime? AssessorRemarksUpdated { get; set; }
        [EmailAddress]
        public string? AssessorEmail { get; set; }
        public string? AssessorRemarks { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }
        public long? EnquiryRequestId { get; set; }
        public EnquiryRequest? EnquiryRequest { get; set; }
        public List<EnquiryRequest> EnquiryRequests { get; set; } = new List<EnquiryRequest>();
        public string? AiSummary { get; set; }
        public DateTime? AiSummaryUpdated { get; set; }
        public string? PdfReportFilePath { get; set; }
        public override string ToString()
        {
            return $"InvestigationReport: " +
                   $"Agent Email={AgentEmail}, " +
                   $"Agent Updated Remarks ={AgentRemarksUpdated}, " +
                   $"Agent Remarks={AgentRemarks}, " +
                   $"Agent Remarks Edit Updated={AgentRemarksEditUpdated}, " +
                   $"Agent Remarks Edit={AgentRemarksEdit}, " +
                   $"Supervisor Remarks Updated={SupervisorRemarksUpdated}, " +
                   $"Supervisor Email={SupervisorEmail}, " +
                   $"Supervisor Remarks={SupervisorRemarks}, " +
                   $"Supervisor Attachment={(SupervisorAttachment != null ? $"[{SupervisorAttachment.Length} bytes]" : "null")}, " +
                   $"Supervisor FileName={SupervisorFileName}, " +
                   $"Supervisor FileType={SupervisorFileType}, " +
                   $"Supervisor FileExtension={SupervisorFileExtension}, " +
                   $"Supervisor RemarkType={SupervisorRemarkType}, " +
                   $"Assessor Remarks Updated={AssessorRemarksUpdated}, " +
                   $"Assessor Email={AssessorEmail}, " +
                   $"Assessor Remarks={AssessorRemarks}, " +
                   $"Assessor RemarkType={AssessorRemarkType}, " +
                   $"Enquiry RequestId={EnquiryRequestId}, " +
                   $"Enquiry Request={(EnquiryRequest != null ? EnquiryRequest.ToString() : "null")}, " +
                   $"Enquiry Requests Count={EnquiryRequests?.Count}, " +
                   $"Ai Summary={AiSummary}, " +
                   $"Ai Summary Updated={AiSummaryUpdated}, " +
                   $"Report={ReportTemplate.ToString()}";
        }

    }
}
