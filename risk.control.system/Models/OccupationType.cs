using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class OccupationType : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Display(Name = "Occupation name")]
        [Required]
        public string Name { get; set; } = default!;

        [Display(Name = "Occupation code")]
        [Required]
        public string Code { get; set; } = default!;
        public override string ToString()
        {
            return $"Occupation Information:\n" +
                $"- Name: {Name}\n" +
                $"- Code: {Code}";
        }
    }
}
