using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ServiceReportTemplate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ServiceReportTemplateId { get; set; }

        public string Name { get; set; }

        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Line of Business")]
        public long? LineOfBusinessId { get; set; }

        [Display(Name = "Line of Business")]
        public virtual LineOfBusiness? LineOfBusiness { get; set; }

        [Display(Name = "Service type")]
        public long? InvestigationServiceTypeId { get; set; }

        [Display(Name = "Service type")]
        public virtual InvestigationServiceType? InvestigationServiceType { get; set; }

        [Display(Name = "Report type")]
        public long? ReportTemplateId { get; set; }

        public ReportTemplate? ReportTemplate { get; set; }
    }

    public class ReportTemplate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReportTemplateId { get; set; }

        public string? Name { get; set; }

        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        public long? DigitalIdReportId { get; set; }

        [Display(Name = "Digital Id report")]
        public virtual DigitalIdReport? DigitalIdReport { get; set; }

        public long? DocumentIdReportId { get; set; }

        [Display(Name = "Document Id report")]
        public virtual DocumentIdReport? DocumentIdReport { get; set; }

        public long? ReportQuestionaireId { get; set; }

        [Display(Name = "Questionaire")]
        public ReportQuestionaire? ReportQuestionaire { get; set; }
    }

    public class ReportQuestionaire : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReportQuestionaireId { get; set; }

        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Questionaire")]
        public string? Question { get; set; }

        public string? Answer { get; set; }

        [Display(Name = "Questionaire type")]
        public string? Type { get; set; }

        public bool Optional { get; set; } = true;
        public string? Question1 { get; set; }
        public string? Answer1 { get; set; }
        public string? Question2 { get; set; }
        public string? Answer2 { get; set; }
        public string? Question3 { get; set; }
        public string? Answer3 { get; set; }
        public string? Question4 { get; set; }
        public string? Answer4 { get; set; }
    }
}