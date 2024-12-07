using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ServicedPinCode : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ServicedPinCodeId { get; set; }

        public string Name { get; set; } = default!;
        public string Pincode { get; set; } = default!;

        [Display(Name = "Vendor services")]
        public long VendorInvestigationServiceTypeId { get; set; } = default!;

        public VendorInvestigationServiceType VendorInvestigationServiceType { get; set; } = default!;
        public override string ToString()
        {
            return $"Investigation ServiceType Information:\n" +
                $"- Name: {Name}\n" +
                $"- Pincode: {Pincode}\n" +
                $"- VendorInvestigationServiceType: {VendorInvestigationServiceType}";
        }
    }
}