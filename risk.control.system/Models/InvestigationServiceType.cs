using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class InvestigationServiceType : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long InvestigationServiceTypeId { get; set; }

        [Display(Name = "InvestigationService Type")]
        [Required]
        public string Name { get; set; } = default!;

        [Display(Name = "InvestigationService Type code")]
        [Required]
        public string Code { get; set; } = default!;
        public InsuranceType? InsuranceType { get; set; }

       public bool MasterData { get; set; } = false;

        public override string ToString()
        {
            return $"Investigation Service Type Information:\n" +
           $"- Name: {Name}\n" +
           $"- Code: {Code}";
        }
    }
}