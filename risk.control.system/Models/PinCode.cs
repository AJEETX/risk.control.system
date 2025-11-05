using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class PinCode : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PinCodeId { get; set; }

        [Display(Name = "PinCode name")]
        public string Name { get; set; } = default!;

        [Display(Name = "PinCode")]
        public string Code { get; set; } = default!;

        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;

        [Display(Name = "State name")]
        public long? StateId { get; set; } = default!;

        [Display(Name = "State name")]
        public State? State { get; set; } = default!;

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country? Country { get; set; } = default!;

        [NotMapped]
        public long SelectedPincodeId { get; set; }
        [NotMapped]
        public long SelectedDistrictId { get; set; }
        [NotMapped]
        public long SelectedStateId { get; set; }
        [NotMapped]
        public long SelectedCountryId { get; set; }
        public override string ToString()
        {
            return $"Pincode Information:\n" +
           $"- Name: {Name}\n" +
           $"- pincode: {Code}\n" +
           $"- Latitude: ${Latitude}\n" +
           $"- Longitude: {Longitude}\n" +
           $"- City: {District}\n" +
           $"- State: {State}\n" +
           $"- Country: {Country}";
        }
    }
}