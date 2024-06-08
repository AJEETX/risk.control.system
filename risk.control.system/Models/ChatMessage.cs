using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ChatMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ChatMessageId { get; set; }
        public string FromUserEmail { get; set; }
        public ApplicationUser? FromUser { get; set; }
        public ApplicationUser? ToUser { get; set; }
        public List<string> FromUserMessage { get; set; }= new List<string>();
        public List<string> ToUserMessage { get; set; } = new List<string>();
    }
}
