using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class AnnualIncome : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Display(Name = "Income name")]
        [Required]
        public string Name { get; set; } = default!;

        [Display(Name = "Income code")]
        [Required]
        public string Code { get; set; } = default!;
        public override string ToString()
        {
            return $"Income Information:\n" +
                $"- Name: {Name}\n" +
                $"- Code: {Code}";
        }
    }
}
