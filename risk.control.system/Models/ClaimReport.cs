using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClaimReportBase : BaseEntity
    {
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public DigitalIdReport? DigitalIdReport { get; set; } = new DigitalIdReport();

        public ReportQuestionaire ReportQuestionaire { get; set; } = new ReportQuestionaire();

        public DocumentIdReport? PanIdReport { get; set; } = new DocumentIdReport();
        public DocumentIdReport? AudioReport { get; set; } = new DocumentIdReport();
        public DocumentIdReport? VideoReport { get; set; } = new DocumentIdReport();
        public DocumentIdReport? PassportIdReport { get; set; } = new DocumentIdReport();

        public string? AgentEmail { get; set; }
        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }
        public DateTime? SupervisorRemarksUpdated { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorRemarks { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }
        public DateTime? AssessorRemarksUpdated { get; set; }
        public string? AssessorEmail { get; set; }
        public string? AssessorRemarks { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }

        public string ClaimsInvestigationId { get; set; }
        public ClaimsInvestigation ClaimsInvestigation { get; set; }
        public virtual ServiceReportTemplate? ServiceReportTemplate { get; set; }

        public override string ToString()
        {
            return $"Claim Report Information:\n" +
                $"- Agent Email: {AgentEmail}\n" +
                $"- Agent Remarks Updated time: {AgentRemarksUpdated}\n" +
                $"- Agent Remarks: {AgentRemarks}\n" +
                $"- Supervisor Remarks Updated time: {SupervisorRemarksUpdated}\n" +
                $"- Supervisor Email: {SupervisorEmail}\n" +
                $"- Supervisor Remarks: {SupervisorRemarks}\n" +
                $"- Assessor Remarks Updated time: {AssessorRemarksUpdated}\n" +
                $"- Assessor Email: {AssessorEmail}\n" +
                $"- Assessor Remarks: {AssessorRemarks}\n" +
                $"- Claim Id: {ClaimsInvestigationId}";
        }
    }
}