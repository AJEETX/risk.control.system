using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ServiceReportTemplate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ServiceReportTemplateId { get; set; } = Guid.NewGuid().ToString();

        public string? ClientCompanyId { get; set; }
        public virtual ClientCompany? ClientCompany { get; set; }
        public string? LineOfBusinessId { get; set; }
        public virtual LineOfBusiness? LineOfBusiness { get; set; }
        public string? InvestigationServiceTypeId { get; set; }
        public virtual InvestigationServiceType? InvestigationServiceType { get; set; }
        public string? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
    }

    public class ReportTemplate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ReportTemplateId { get; set; } = Guid.NewGuid().ToString();

        public List<DigitalIdReport> DigitalIdReport { get; set; } = new List<DigitalIdReport>();
        public List<DocumentIdReport> DocumentIdReport { get; set; } = new List<DocumentIdReport>();
        public List<ReportQuestionaire> ReportQuestionaire { get; set; } = new List<ReportQuestionaire>();
    }

    public class ReportQuestionaire : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ReportQuestionaireId { get; set; } = Guid.NewGuid().ToString();

        public string? QuestionName { get; set; }

        public string? QuestionDetail { get; set; }
        public string? QuestionType { get; set; }
        public bool? Optional { get; set; }
    }
}