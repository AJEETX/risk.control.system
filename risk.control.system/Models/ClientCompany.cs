using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClientCompany :BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClientCompanyId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Branch { get; set; } = default!;
        public string Addressline { get; set; } = default!;
        public string City { get; set; } = default!;
        [Display(Name = "State name")]
        public string? StateId { get; set; } = default!;
        public State? State { get; set; } = default!;
        [Display(Name = "Country name")]
        public string? CountryId { get; set; } = default!;
        public Country? Country { get; set; } = default!;
        [Display(Name = "Pincode")]
        public string? PinCodeId { get; set; }= default!;
        public PinCode? PinCode { get; set; } = default!;
        [Display(Name = "District")] 
        public string DistrictId { get; set; } = default!;
        [Display(Name = "District")]
        [Required]
        public District District { get; set; } = default!;

    }
}
