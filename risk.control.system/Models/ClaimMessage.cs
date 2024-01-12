using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClaimMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ClaimMessageId { get; set; }

        public string? SenderEmail { get; set; }
        public string? RecepicientEmail { get; set; }
        public string? Message { get; set; }
        public string? ClaimsInvestigationId { get; set; }
    }
}