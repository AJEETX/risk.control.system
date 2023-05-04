using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ServicedPinCode : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ServicedPinCodeId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = default!;
        public string Pincode { get; set; } = default!;
        public string VendorInvestigationServiceTypeId { get; set; } = default!;
        public VendorInvestigationServiceType VendorInvestigationServiceType { get; set; } = default!;

    }
}
