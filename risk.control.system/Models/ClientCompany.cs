using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Models
{
    public class ClientCompany : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ClientCompanyId { get; set; }

        public Domain? DomainName { get; set; } = Domain.com;

        [Display(Name = "Insurer name")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = default!;

        [Display(Name = "Phone number")]
        [StringLength(13, MinimumLength = 9, ErrorMessage = "PhoneNumber must be between 9 and 13 characters.")]
        public string PhoneNumber { get; set; } = default!;

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Email must be between 3 and 30 characters.")]
        public string Email { get; set; } = default!;

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Branch must be between 3 and 50 characters.")]
        public string? Branch { get; set; } = default!;

        [StringLength(70, MinimumLength = 3, ErrorMessage = "Addressline must be between 3 and 70 characters.")]
        public string Addressline { get; set; } = default!;

        [Display(Name = "State name")]
        public long? StateId { get; set; } = default!;

        public State? State { get; set; } = default!;

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        public Country? Country { get; set; } = default!;

        [Display(Name = "Pincode")]
        public long? PinCodeId { get; set; } = default!;

        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "District")]
        public long? DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;

        [StringLength(50, MinimumLength = 3, ErrorMessage = "BankName must be between 3 and 50 characters.")]
        public string? BankName { get; set; } = default!;

        [Display(Name = "Bank Account Number")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "BankAccountNumber must be between 3 and 50 characters.")]
        public string? BankAccountNumber { get; set; } = default!;

        [StringLength(15, MinimumLength = 6, ErrorMessage = "Code must be between 6 and 15 characters.")]
        public string? IFSCCode { get; set; } = default!;

        [DataType(DataType.Date)]
        public DateTime? AgreementDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        public DateTime? ActivatedDate { get; set; } = DateTime.UtcNow;

        public CompanyStatus? Status { get; set; } = CompanyStatus.ACTIVE;

        public string? DocumentUrl { get; set; } = default!;

        public string? DocumentImageExtension { get; set; } = default!;
        public string? AddressMapLocation { get; set; }
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }
        public List<Vendor>? EmpanelledVendors { get; set; } = new();
        public bool VerifyPan { get; set; } = false;
        public bool VerifyPassport { get; set; } = false;
        public bool EnablePassport { get; set; } = false;
        public bool EnableMedia { get; set; } = false;
        public string PanIdfyUrl { get; set; } = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verification/marketing/pan";
        public string PanAPIData { get; set; } = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        public string PanAPIHost { get; set; } = "pan-card-verification-at-lowest-price.p.rapidapi.com";
        public string? PassportApiUrl { get; set; } = "https://document-ocr1.p.rapidapi.com/idr";
        public string? PassportApiData { get; set; } = "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a";
        public string? PassportApiHost { get; set; } = "document-ocr1.p.rapidapi.com";
        public bool CanChangePassword { get; set; } = false;
        public LicenseType LicenseType { get; set; } = LicenseType.Trial;

        [DataType(DataType.DateTime)]
        public DateTime? ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(10);

        [Range(1, 500)]
        public int TotalCreatedClaimAllowed { get; set; } = 100;

        [Range(1, 500)]
        public int TotalToAssignMaxAllowed { get; set; } = 100;

        public bool Deleted { get; set; } = false;
        public bool HasClaims { get; set; } = false;
        public bool AiEnabled { get; set; } = false;

        public int CreatorSla { get; set; } = 2;
        public int AssessorSla { get; set; } = 4;
        public int SupervisorSla { get; set; } = 2;
        public int AgentSla { get; set; } = 5;
        public bool UpdateAgentAnswer { get; set; } = true;

        public bool HasSampleData { get; set; } = true;

        [NotMapped]
        public IFormFile? Document { get; set; }

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
            return $"Insurance Company Information:\n" +
                $"- Name: {Name}\n" +
                $"- Email: {Email}\n" +
                $"- Branch: {Branch}\n" +
                $"- Address Line: {Addressline}\n" +
                $"- State: {State}\n" +
                $"- Country: {Country}\n" +
                $"- Pincode: {PinCode}\n" +
                $"- District: {District}\n" +
                $"- Bank Name: {BankName}\n" +
                $"- Bank Account Number: {BankAccountNumber}\n" +
                $"- IFSC Code: {IFSCCode}\n" +
                $"- Agreement Date: {AgreementDate}\n" +
                $"- Activated Date: {ActivatedDate}\n" +
                $"- Status: {Status}\n" +
                $"- Document URL: {DocumentUrl}\n" +
                $"- Empanelled Vendors: {EmpanelledVendors}\n" +
                $"- Verify Pan: {VerifyPan}\n" +
                $"- Verify Passport: {VerifyPassport}\n" +
                $"- Enable Passport: {EnablePassport}\n" +
                $"- Enable Media: {EnableMedia}\n" +
                $"- Pan IDfy URL: {PanIdfyUrl}\n" +
                $"- Rapid API Key: {PanAPIData}\n" +
                $"- Rapid API Host: {PanAPIHost}\n" +
                $"- Passport API URL: {PassportApiUrl}\n" +
                $"- Passport API Key: {PassportApiData}\n" +
                $"- Passport API Host: {PassportApiHost}\n" +
                $"- Can Change Password: {CanChangePassword}\n";
        }
    }

    public enum CompanyStatus
    {
        ACTIVE,
        INACTIVE,
    }
}