using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class Vendor : BaseEntity
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
        public List<VendorInvestigationServiceType>? VendorInvestigationServiceTypes { get; set; } = default!;
        [Display(Name = "State name")]
        public string? StateId { get; set; } = default!;
        public State? State { get; set; } = default!;
        [Display(Name = "Country name")]
        public string? CountryId { get; set; } = default!;
        public Country? Country { get; set; } = default!;
        [Display(Name = "Pincode")]
        public string? PinCodeId { get; set; } = default!;
        public PinCode? PinCode { get; set; } = default!;
        [Display(Name = "District")]
        public string? DistrictId { get; set; } = default!;
        [Display(Name = "District")]
        public District? District { get; set; } = default!;
        [Display(Name = "Bank Name")]
        public string BankName { get; set; } = default!;
        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; } = default!;
        public string IFSCCode { get; set; } = default!;
        public string City { get; set; } = "KANPUR CITY";
        public DateTime? AgreementDate { get; set; } = DateTime.Now;
        public DateTime? ActivatedDate { get; set; } = DateTime.Now;
        public DateTime? DeListedDate { get; set; }
        public VendorStatus? Status { get; set; } = VendorStatus.ACTIVE;
        public string? DelistReason { get; set; } = default!;

        public string? DocumentUrl { get; set; } = default!;
        [Display(Name = "Document")]
        [NotMapped]
        public IFormFile? Document { get; set; }
        [Display(Name = "Document url")]
        public byte[]? DocumentImage { get; set; } = default!;

        public List<VendorApplicationUser>? VendorApplicationUser { get; set; }
        public string? ClientCompanyId { get; set; }
        public ClientCompany? ClientCompany { get; set; }
    }
    public enum VendorStatus
    {
        ACTIVE,
        INACTIVE,
        DELIST
    }
}
