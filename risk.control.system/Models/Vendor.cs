using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class Vendor :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VendorId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Addressline { get; set; } = string.Empty;
        public List<VendorInvestigationServiceType> VendorInvestigationServiceTypes { get; set; }
        public string City { get; set; } = string.Empty;
        [Display(Name = "State name")]
        public string? StateId { get; set; }
        public State? State { get; set; }
        [Display(Name = "Country name")]
        public string? CountryId { get; set; }
        public Country? Country { get; set; }
        [Display(Name = "Pincode")]
        public string? PinCodeId { get; set; }
        public PinCode? PinCode { get; set; }
        [Display(Name = "District")]
        public string? DistrictId { get; set; }
        public District? District { get; set; }
        public List<VendorUser>? VendorUsers { get; set; }
    }

    public class VendorInvestigationServiceType :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VendorInvestigationServiceTypeId { get; set; } = Guid.NewGuid().ToString();
        [Display(Name = "Line of business")]
        public string LineOfBusinessId { get; set; }
        public LineOfBusiness LineOfBusiness { get; set; }
        [Display(Name = "Investigation service")]
        public string InvestigationServiceTypeId { get; set; }
        public InvestigationServiceType InvestigationServiceType { get; set; }
        [Display(Name = "State name")]
        public string StateId { get; set; }
        public State State { get; set; }
        public List<ServicedPinCode> ServicedPinCodes { get; set; }
    }

    public class ServicedPinCode :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServicedPinCodeId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
