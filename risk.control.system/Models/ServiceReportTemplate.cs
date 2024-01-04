using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ServiceReportTemplate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ServiceReportTemplateId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }

        [Display(Name = "Insurer name")]
        public string? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Line of Business")]
        public string? LineOfBusinessId { get; set; }

        [Display(Name = "Line of Business")]
        public virtual LineOfBusiness? LineOfBusiness { get; set; }

        [Display(Name = "Service type")]
        public string? InvestigationServiceTypeId { get; set; }

        [Display(Name = "Service type")]
        public virtual InvestigationServiceType? InvestigationServiceType { get; set; }

        [Display(Name = "Report type")]
        public string? ReportTemplateId { get; set; }

        public ReportTemplate? ReportTemplate { get; set; }
    }

    public class ReportTemplate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ReportTemplateId { get; set; } = Guid.NewGuid().ToString();

        public string? Name { get; set; }

        [Display(Name = "Insurer name")]
        public string? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        public string? DigitalIdReportId { get; set; }

        [Display(Name = "Digital Id report")]
        public DigitalIdReport? DigitalIdReport { get; set; }

        public string? DocumentIdReportId { get; set; }

        [Display(Name = "Document Id report")]
        public DocumentIdReport? DocumentIdReport { get; set; }

        public string? ReportQuestionaireId { get; set; }

        [Display(Name = "Questionaire")]
        public ReportQuestionaire? ReportQuestionaire { get; set; }
    }

    public class ReportQuestionaire : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ReportQuestionaireId { get; set; } = Guid.NewGuid().ToString();

        [Display(Name = "Insurer name")]
        public string? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Questionaire")]
        public string? Question { get; set; }

        public string? Answer { get; set; }

        [Display(Name = "Questionaire type")]
        public string? Type { get; set; }

        public bool Optional { get; set; } = true;
        public string? Question1 { get; set; }
        public string? Question2 { get; set; }
        public string? Question3 { get; set; }
        public string? Question4 { get; set; }
    }
}