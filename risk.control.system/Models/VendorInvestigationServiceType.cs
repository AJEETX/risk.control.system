using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class VendorInvestigationServiceType : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VendorInvestigationServiceTypeId { get; set; } = Guid.NewGuid().ToString();
        [Display(Name = "Investigation service type")]
        public string InvestigationServiceTypeId { get; set; } = default!;
        [Display(Name = "Investigation service type")]
        public InvestigationServiceType InvestigationServiceType { get; set; } = default!;
        [Display(Name = "Line of busuness")]
        public string LineOfBusinessId { get; set; } = default!;
        public LineOfBusiness LineOfBusiness { get; set; } = default!;

        [Display(Name = "State")]
        public string StateId { get; set; } = default!;
        public State State { get; set; } = default!;

        [Display(Name = "District")]
        public string? DistrictId { get; set; } = default!;
        public District? District { get; set; } = default!;

        public decimal Price { get; set; }
        [NotMapped]
        [Display(Name = "Choose Multiple Pincodes")]
        public List<string> SelectedMultiPincodeId { get; set; } = default!;
        public List<ServicedPinCode> PincodeServices { get; set; } = new List<ServicedPinCode> { new ServicedPinCode { } };

        public string VendorId { get; set; }
        public Vendor Vendor { get; set; }
    }
}
