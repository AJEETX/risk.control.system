using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ContactMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ContactMessageId { get; set; } = Guid.NewGuid().ToString();
        public string SenderEmail { get; set; }
        public string ReceipientEmail { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; } = false;
        public ContactMessagePriority Priority { get; set; } = ContactMessagePriority.NORMAL;
        [Display(Name = "Send date")]
        public DateTime? SendDate { get; set; }
        [Display(Name = "Received date")]
        public DateTime? ReceiveDate { get; set; }
        [Display(Name = "Attachments")]
        public List<FileAttachment>? Attachments { get; set; }

        [Display(Name = "User")]
        public long? ApplicationUserId { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }
        [NotMapped]
        public bool? SelectedForAction { get; set; }
        public bool? IsDraft { get; set; }
        public bool? Trashed { get; set; }
        public bool? DeleteTrashed { get; set; }
        public MessageStatus MessageStatus { get; set; } = MessageStatus.NONE;
    }

    public enum ContactMessagePriority
    {
        [Display(Name = "urgent")] URGENT,
        [Display(Name = "important")] IMPORTANT,
        [Display(Name = "high")] HIGH,
        [Display(Name = "normal")] NORMAL,
        [Display(Name = "other")] OTHER,
    }

    public enum MessageStatus
    {
        NONE,
        CREATED,
        DRAFTED,
        SENT,
        RECEIVED,
        DELETED,
        TRASHDELETED
    }
}
