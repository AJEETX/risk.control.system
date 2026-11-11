using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class SmsForCase : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long? ClientCompanyId { get; set; }
        [EmailAddress]
        public string SenderEmail { get; set; } = default!;
        [EmailAddress]
        public string RecepientEmail { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string Message { get; set; } = default!;
        public long? InvestigationTaskId { get; set; }
    }
}
