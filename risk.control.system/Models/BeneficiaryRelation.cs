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
        public string Name { get; set; } = default!;
        [Display(Name = "Beneficiary relation code")]
        [Required]
        public string Code { get; set; } = default!;
        public bool IsUpdated { get; set; } = true;

        public override string ToString()
        {
            return $"Beneficiary relation Information:\n" +
                $"- Name: {Name}\n" +
                $"- Code: {Code}";
        }
    }
}
