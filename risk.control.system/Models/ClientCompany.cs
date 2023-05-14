using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ClientCompany : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClientCompanyId { get; set; } = Guid.NewGuid().ToString();
        [Display(Name = "Company name")]
        public string Name { get; set; } = default!;
        [Display(Name = "Company code")]
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Branch { get; set; } = default!;
        public string Addressline { get; set; } = default!;
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

        public string BankName { get; set; } = default!;
        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; } = default!;
        public string IFSCCode { get; set; } = default!;
        public DateTime? AgreementDate { get; set; } = DateTime.Now;
        public DateTime? ActivatedDate { get; set; } = DateTime.Now;
        public CompanyStatus? Status { get; set; } = CompanyStatus.ACTIVE;

        public string? DocumentUrl { get; set; } = default!;
        [Display(Name = "Document")]
        [NotMapped]
        public IFormFile? Document { get; set; }
        [Display(Name = "Document url")]
        public byte[]? DocumentImage { get; set; } = default!;
        public List<ClientCompanyApplicationUser>? VendorApplicationUser { get; set; }

        public List<Vendor>? EmpanelledVendors { get; set; } = new();
        public List<ClaimsInvestigation> ClaimsInvestigations { get; set; } = new();
    }
    public enum CompanyStatus
    {
        ACTIVE,
        INACTIVE,

    }
}
