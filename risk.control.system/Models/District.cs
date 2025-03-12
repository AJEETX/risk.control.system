using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class District : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DistrictId { get; set; }

        [Display(Name = "District name")]
        public string Name { get; set; } = "ALL DISTRICTS";

        [Display(Name = "District code")]
        [Required]
        public string Code { get; set; } = default!;

        [Display(Name = "State name")]
        public long? StateId { get; set; } = default!;

        [Display(Name = "State name")]
        public State? State { get; set; } = default!;

        [Required]
        [Display(Name = "Country name")]
        public long CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country Country { get; set; } = default!;

        [NotMapped]
        public long SelectedDistrictId { get; set; }
        [NotMapped]
        public long SelectedStateId { get; set; }
        [NotMapped]
        public long SelectedCountryId { get; set; }
        public override string ToString()
        {
            return $"City Information:\n" +
           $"- Name: {Name}\n" +
           $"- City code: {Code}\n" +
           $"- State: ${State}\n" +
           $"- Country: {Country}";
        }
    }
}