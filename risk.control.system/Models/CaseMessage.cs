using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string? SenderEmail { get; set; }
        public string? SenderPhone { get; set; }
        public string? RecepicientEmail { get; set; }
        public string? RecepicientPhone { get; set; }
        public DateTime? ScheduleTime { get; set; }
        public string? Message { get; set; }
        public long? InvestigationTaskId { get; set; }
        public long? PreviousCaseMessageId { get; set; }
        public override string ToString()
        {
            return $"Claim Message Information:\n" +
                $"- Sender Email: {SenderEmail}\n" +
                $"- Sender Phone: {SenderPhone}\n" +
                $"- Recepicient Email: {RecepicientEmail}\n" +
                $"- Recepicient Phone: {RecepicientPhone}\n" +
                $"- Schedule Time: {ScheduleTime}\n" +
                $"- Message: {Message}\n";
        }
    }
}