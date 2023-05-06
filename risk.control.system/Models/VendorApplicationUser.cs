namespace risk.control.system.Models
{
    public class VendorApplicationUser :ApplicationUser
    {
        public string? VendorId { get; set; } = default!;
        public Vendor? Vendor { get; set; } = default!;
        public string? Comments { get; set; } = default!;
    }
}
