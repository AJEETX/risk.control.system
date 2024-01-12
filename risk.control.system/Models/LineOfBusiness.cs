using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class LineOfBusiness : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LineOfBusinessId { get; set; }

        [Display(Name = "Line of Business")]
        [Required]
        public string Name { get; set; } = default!;

        [Display(Name = "Line of Business code")]
        [Required]
        public string Code { get; set; } = default!;

        public List<InvestigationServiceType>? InvestigationServiceTypes { get; set; } = default!;
        public bool MasterData { get; set; } = false;
    }
}