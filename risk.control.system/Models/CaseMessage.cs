using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [EmailAddress]
        public string? SenderEmail { get; set; }

        [EmailAddress]
        public string? RecepicientEmail { get; set; }

        public bool IsCustomer { get; set; } = false;

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Message must be between 3 and 50 characters.")]
        public string? Message { get; set; }

        public long? InvestigationTaskId { get; set; }

        public override string ToString()
        {
            return $"Claim Message Information:\n" +
                $"- Sender Email: {SenderEmail}\n" +
                $"- Recepicient Email: {RecepicientEmail}\n" +
                $"- Message: {Message}\n";
        }
    }
}