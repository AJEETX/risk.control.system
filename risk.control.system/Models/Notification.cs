using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class StatusNotification
    {
        public int StatusNotificationId { get; set; }
        public ApplicationRole? Role { get; set; }
        public ClientCompany? Company { get; set; }
        public Vendor? Agency { get; set; }

        public string? UserEmail { get; set; }
        public string Message { get; set; }
        public bool IsReadByCreator { get; set; } = false;
        public bool IsReadByVendor { get; set; } = false;
        public bool IsReadByVendorAgent { get; set; } = false;
        public bool IsReportReadByVendor { get; set; } = false;
        public bool IsReportReadByAssessor { get; set; } = false;
        public bool IsReportReadByManager { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
