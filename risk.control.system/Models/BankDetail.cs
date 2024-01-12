using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class BankDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BankDetailId { get; set; }

        [Display(Name = "Bank Name")]
        public string Name { get; set; } = default!;

        [Display(Name = "Bank Account Number")]
        public string AccountNumber { get; set; } = default!;

        public string IFSCCode { get; set; } = default!;
    }
}