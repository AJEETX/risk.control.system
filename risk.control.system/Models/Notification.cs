namespace risk.control.system.Models
{
    public class StatusNotification
    {
        public int StatusNotificationId { get; set; }
        public ApplicationRole? Role { get; set; }
        public string? Status { get; set; }
        public ClientCompany? Company { get; set; }
        public Vendor? Agency { get; set; }
        public string? NotifierUserEmail { get; set; }
        public string? AgenctUserEmail { get; set; }
        public string Message { get; set; }
        public bool IsReadByCreator { get; set; } = false;
        public bool IsReadByVendor { get; set; } = false;
        public bool IsReadByAssessor { get; set; } = false;
        public bool IsReadByVendorAgent { get; set; } = false;
        public bool IsReadByManager { get; set; } = false;

        public string Symbol { get; set; } = "fas fa-check-circle i-green";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
    }
}
