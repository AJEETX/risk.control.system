using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ContactUsMessage : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
        public ContactUSMessagePriority Priority { get; set; } = ContactUSMessagePriority.NORMAL;
        public DateTime? SendDate { get; set; }
        public DateTime? ReceiveDate { get; set; }
        [NotMapped]
        public IFormFile? Attachment { get; set; }
        public byte[]? AttachedDocument { get; set; }

    }
    public enum ContactUSMessagePriority
    {
        [Display(Name = "urgent")] URGENT,
        [Display(Name = "important")] IMPORTANT,
        [Display(Name = "high")] HIGH,
        [Display(Name = "normal")] NORMAL,
        [Display(Name = "other")] OTHER,
    }
}
