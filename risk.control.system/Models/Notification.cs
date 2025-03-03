using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public ApplicationRole? Role { get; set; }
        public ClientCompany? Company { get; set; }
        public Vendor? Agency { get; set; }

        public string? UserEmail { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
