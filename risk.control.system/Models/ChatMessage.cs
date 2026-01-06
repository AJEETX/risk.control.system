using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ChatMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ChatMessageId { get; set; }
        [EmailAddress]
        public string FromUserEmail { get; set; }
        public ApplicationUser? FromUser { get; set; }
        public ApplicationUser? ToUser { get; set; }
        public List<string>? ActiveUsers { get; set; }

        public override string ToString()
        {
            return $"ChatMessage Information:\n" +
                $"Sms From User:{FromUser}\n" +
                $"- From User Email: {FromUserEmail}";
        }
    }
}
