using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using risk.control.system.AppConstant;

namespace risk.control.system.Models
{
    public class VendorApplicationUser : ApplicationUser
    {
        [Display(Name = "Vendor code")]
        public long? VendorId { get; set; } = default!;
        public AgencyRole? UserRole { get; set; }
        public Vendor? Vendor { get; set; } = default!;
        public string? Comments { get; set; } = default!;
        [NotMapped]
        public IList<AgencyRole> AgencyRole { get; set; } = new List<AgencyRole>();
    }
}