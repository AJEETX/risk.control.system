using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Standard.Licensing;

namespace risk.control.system.Models
{
    public class ClientCompany : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public string Name { get; set; } = default!;

        [Display(Name = "Insurer code")]
        public string Code { get; set; } = default!;

        public string Description { get; set; } = default!;

        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = default!;

        public string Email { get; set; } = default!;
        public string Branch { get; set; } = default!;
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

        public string BankName { get; set; } = default!;

        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; } = default!;

        public string IFSCCode { get; set; } = default!;

        [DataType(DataType.Date)]
        public DateTime? AgreementDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime? ActivatedDate { get; set; } = DateTime.Now;

        public CompanyStatus? Status { get; set; } = CompanyStatus.ACTIVE;

        public string? DocumentUrl { get; set; } = default!;

        [Display(Name = "Document url")]
        public byte[]? DocumentImage { get; set; } = default!;

        public List<ClientCompanyApplicationUser>? CompanyApplicationUser { get; set; }

        public List<Vendor>? EmpanelledVendors { get; set; } = new();
        public bool AutoAllocation { get; set; } = false;
        public bool VerifyPan { get; set; } = false;
        public bool VerifyPassport { get; set; } = false;
        public bool EnablePassport { get; set; } = false;
        public string ApiBaseUrl { get; set; } = "https://2j2sgigd3l.execute-api.ap-southeast-2.amazonaws.com/Development/icheckify";
        public string PanIdfyUrl { get; set; } = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verification/marketing/pan";
        public string RapidAPIKey { get; set; } = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        public string RapidAPIHost { get; set; } = "pan-card-verification-at-lowest-price.p.rapidapi.com";
        public string? RapidAPIPanRemainCount { get; set; }
        public string? PassportApiUrl { get; set; } = "https://document-ocr1.p.rapidapi.com/idr";
        public string? PassportApiKey { get; set; } = "327fd8beb9msh8a441504790e80fp142ea8jsnf74b9208776a";
        public string? PassportApiHost { get; set; } = "document-ocr1.p.rapidapi.com";
        public bool SendSMS { get; set; } = false;
        public bool CanChangePassword { get; set; } = false;
        public string MobileAppUrl { get; set; } = "https://apkichck.s3.ap-southeast-2.amazonaws.com/icheckify/app-release.apk";
        public bool BulkUpload { get; set; } = false;
        public string WhitelistIpAddress { get; set; } = "::1;202.7.251.159";
        public string? WhitelistIpAddressRange { get; set; } = default!;
        public LicenseType LicenseType { get; set; } = LicenseType.Trial;
        public string LicenseId { get; set; } = Guid.NewGuid().ToString();

        [DataType(DataType.DateTime)]
        public DateTime? ExpiryDate { get; set; } = DateTime.Now.AddDays(10);
        [Range(5, 50)]
        public int TotalCreatedClaimAllowed { get; set; } = 10;
        public bool Deleted { get; set; } = false;
        public bool HasClaims { get; set; } = false;
    }

    public enum CompanyStatus
    {
        ACTIVE,
        INACTIVE,
    }

    public enum Allocation
    {
        MANUAL,
        AUTO
    }
}