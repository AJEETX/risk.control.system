using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class SmsNotification : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SmsNotificationId { get; set; }

        public string? RecepicientPhone { get; set; }
        public string? RecepicientEmail { get; set; }
        public string? Message { get; set; }
        public string? ClaimsInvestigationId { get; set; }
        public override string ToString()
        {
            return $"Claim Message Information:\n" +
                $"- Recepicient Phone: {RecepicientPhone}\n" +
                $"- Message: {Message}\n" +
                $"- Claim Id: {ClaimsInvestigationId}";
        }
    }
}