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
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = default!;

        [Display(Name = "Case enabler code")]
        [Required]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 20 characters.")]
        public string Code { get; set; } = default!;

        public override string ToString()
        {
            return $"Case enabler Information:\n" +
                $"- Name: {Name}\n" +
                $"- Code: {Code}";
        }
    }
}