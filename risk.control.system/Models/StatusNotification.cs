namespace risk.control.system.Models
{
    public class StatusNotification
    {
        public long StatusNotificationId { get; set; }
        public long? RoleId { get; set; }
        public ApplicationRole? Role { get; set; }
        public string? Status { get; set; }
        public long? ClientCompanyId { get; set; }
        public ClientCompany? Company { get; set; }
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public string? NotifierUserEmail { get; set; }
        public string? AgenctUserEmail { get; set; }
        public string Message { get; set; }
        public bool IsReadByCreator { get; set; } = false;
        public bool IsReadByVendor { get; set; } = false;
        public bool IsReadByAssessor { get; set; } = false;
        public bool IsReadByVendorAgent { get; set; } = false;
        public bool IsReadByManager { get; set; } = false;

        public string Symbol { get; set; } = "fas fa-check-circle i-green";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}