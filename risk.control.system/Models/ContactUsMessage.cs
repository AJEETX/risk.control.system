using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ContactMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ContactMessageId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
        public ContactMessagePriority Priority { get; set; } = ContactMessagePriority.NORMAL;
        [Display(Name = "Send date")]
        public DateTime? SendDate { get; set; }
        [Display(Name = "Received date")]
        public DateTime? ReceiveDate { get; set; }
        [Display(Name = "Attachments")]
        public List<FileAttachment>? Attachments { get; set; }

        [Display(Name = "User")]
        public long? ApplicationUserId { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
        [NotMapped]
        public bool? SelectedForAction { get; set; }

    }

    public enum ContactMessagePriority
    {
        [Display(Name = "urgent")] URGENT,
        [Display(Name = "important")] IMPORTANT,
        [Display(Name = "high")] HIGH,
        [Display(Name = "normal")] NORMAL,
        [Display(Name = "other")] OTHER,
    }
}
