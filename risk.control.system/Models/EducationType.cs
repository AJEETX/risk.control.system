using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class EducationType : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Display(Name = "Education name")]
        [Required]
        public string Name { get; set; } = default!;

        [Display(Name = "Education code")]
        [Required]
        public string Code { get; set; } = default!;
        public override string ToString()
        {
            return $"Education Information:\n" +
                $"- Name: {Name}\n" +
                $"- Code: {Code}";
        }
    }
}
