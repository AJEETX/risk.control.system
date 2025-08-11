using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Mvc.Rendering;

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

        public InsuranceType? InsuranceType { get; set; }

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        public Country? Country { get; set; } = default!;

        [Display(Name = "State")]
        public long? StateId { get; set; } = default!;

        public State? State { get; set; } = default!;

        public bool AllDistrictsCheckbox { get; set; }
        public List<long> SelectedDistrictIds { get; set; } = new List<long>();

        [NotMapped]
        public List<SelectListItem> DistrictList { get; set; } = new();
        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        public District? District { get; set; } = default!;

        [NotMapped]
        public long SelectedDistrictId { get; set; }
        [NotMapped]
        public long SelectedStateId { get; set; }
        [NotMapped]
        public long SelectedCountryId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public long VendorId { get; set; }
        public Vendor Vendor { get; set; }
        public bool Deleted { get; set; } = false;
        public override string ToString()
        {
            return $"Agency Investigation Service Type Information:\n" +
                $"- Investigation Service Type: {InvestigationServiceType}\n" +
                $"- Country: {Country}\n" +
                $"- State: {State}\n" +
                $"- District: {District}\n" +
                $"- Price: {Price}\n" +
                $"- Vendor: {Vendor}\n" +
                $"- Deleted: {Deleted}";
        }
    }
}