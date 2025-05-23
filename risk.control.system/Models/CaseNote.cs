using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseNote : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Sender { get; set; }
        public string Comment { get; set; }
        public ClaimNote? ParentClaimNote { get; set; }
        public override string ToString()
        {
            return $"CaseNote Information:\n" +
            $"- Sender: {Sender}\n" +
            $"- Comment: {Comment}";
        }
    }
}