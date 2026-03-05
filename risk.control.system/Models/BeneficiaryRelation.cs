using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class BeneficiaryRelation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BeneficiaryRelationId { get; set; } = default!;

        [Display(Name = "Beneficiary relation name")]
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = default!;

        [Display(Name = "Beneficiary relation code")]
        [Required]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 20 characters.")]
        public string Code { get; set; } = default!;

        public override string ToString()
        {
            return $"Beneficiary relation Information:\n" +
                $"- Name: {Name}\n" +
                $"- Code: {Code}";
        }
    }
}