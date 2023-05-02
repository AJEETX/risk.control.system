namespace risk.control.system.Models
{
    public class VendorUser : ApplicationUser
    {
        public string? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public string? Comments { get; set; }
    }
}
