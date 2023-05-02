using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class LineOfBusiness :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string LineOfBusinessId { get; set; } = Guid.NewGuid().ToString();
        [Display(Name = "Line of Business")]
        [Required]
        public string Name { get; set; }
        [Display(Name = "Line of Business code")]
        [Required]
        public string Code { get; set; }
        public List<InvestigationServiceType>? InvestigationServiceTypes { get; set; }
    }
}
