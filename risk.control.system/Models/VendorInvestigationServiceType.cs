using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class VendorInvestigationServiceType : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VendorInvestigationServiceTypeId { get; set; }

        [Display(Name = "Investigation service type")]
        public long InvestigationServiceTypeId { get; set; } = default!;

        [Display(Name = "Investigation service type")]
        public InvestigationServiceType InvestigationServiceType { get; set; } = default!;

        [Display(Name = "Line of business")]
        public long? LineOfBusinessId { get; set; } = default!;

        public LineOfBusiness? LineOfBusiness { get; set; } = default!;

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        public Country? Country { get; set; } = default!;

        [Display(Name = "State")]
        public long? StateId { get; set; } = default!;

        public State? State { get; set; } = default!;

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        public District? District { get; set; } = default!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [NotMapped]
        [Display(Name = "Choose Multiple Pincodes")]
        public List<long> SelectedMultiPincodeId { get; set; } = new List<long> { }!;

        [Display(Name = "Serviced pincodes")]
        public List<ServicedPinCode> PincodeServices { get; set; } = default!;

        public long VendorId { get; set; }
        public Vendor Vendor { get; set; }
        public bool Deleted { get; set; } = false;
    }
}