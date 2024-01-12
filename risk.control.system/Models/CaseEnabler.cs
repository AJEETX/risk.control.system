using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseEnabler : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CaseEnablerId { get; set; }

        [Display(Name = "Case enabler name")]
        [Required]
        public string Name { get; set; } = default!;

        [Display(Name = "Case enabler code")]
        [Required]
        public string Code { get; set; } = default!;
    }
}