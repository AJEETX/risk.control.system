using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class InvestigationCase :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string InvestigationId { get; set; } = Guid.NewGuid().ToString(); 
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        [Display(Name = "Line of Business")]
        public string LineOfBusinessId { get; set; }
        [Display(Name = "Line of Business")]
        public LineOfBusiness LineOfBusiness { get; set; }
        public string InvestigationServiceTypeId { get; set; }
        public InvestigationServiceType InvestigationServiceType { get; set; }
        [Required]
        [Display(Name = "Case status")]
        public string InvestigationCaseStatusId { get; set; }
        [Display(Name = "Case status")]
        public InvestigationCaseStatus InvestigationCaseStatus { get; set; }
    }

    public class ClaimsInvestigation :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimsInvestigationCaseId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        [Display(Name = "Line of Business")]
        public string LineOfBusinessId { get; set; }
        [Display(Name = "Line of Business")]
        public LineOfBusiness LineOfBusiness { get; set; }
        public List<InvestigationServiceType>? InvestigationServiceTypes { get; set; }
        [Required]
        [Display(Name = "Case status")]
        public string InvestigationCaseStatusId { get; set; }
        [Display(Name = "Case status")]
        public InvestigationCaseStatus InvestigationCaseStatus { get; set; }

    }
}
