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
        public DocumentIdReport? AudioReport { get; set; } = new();
        public DocumentIdReport? VideoReport { get; set; } = new();

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
        public override string ToString()
        {
            return $"Agency Report Information:\n" +
                $"- Agent Email: {AgentEmail}\n" +
                $"- Agent Remarks Updatedtime: {AgentRemarksUpdated}\n" +
                $"- Agent Remarks: {AgentRemarks}\n" +
                $"- Supervisor Remarks Updated time: {SupervisorRemarksUpdated}\n" +
                $"- Supervisor Email: {SupervisorEmail}\n" +
                $"- Supervisor Remarks: {SupervisorRemarks}\n" +
                $"- Supervisor Attachment: {SupervisorAttachment}\n" +
                $"- Supervisor Attached FileName: {SupervisorFileName}\n" +
                $"- Supervisor Attached FileType: {SupervisorFileType}\n" +
                $"- Supervisor Attached FileExtension: {SupervisorFileExtension}\n" +
                $"- Assessor Remarks Updated time: {AssessorRemarksUpdated}\n" +
                $"- Assessor Email: {AssessorEmail}\n" +
                $"- Assessor Remarks: {AssessorRemarks}\n" +
                $"- Claim Id: {ClaimsInvestigationId}";
        }
    }
}
