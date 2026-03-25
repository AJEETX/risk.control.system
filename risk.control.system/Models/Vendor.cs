using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Models
{
    public class Vendor : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VendorId { get; set; }

        [Required]
        [Display(Name = "Agency name")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Name must be between 5 and 50 characters.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(70, MinimumLength = 3, ErrorMessage = "Addressline must be between 3 and 70 characters.")]
        public string Addressline { get; set; } = string.Empty;

        public List<VendorInvestigationServiceType>? VendorInvestigationServiceTypes { get; set; } = default!;

        public long? StateId { get; set; } = default!;

        public State? State { get; set; } = default!;

        public long? CountryId { get; set; } = default!;

        public Country? Country { get; set; } = default!;

        public long? PinCodeId { get; set; } = default!;

        public PinCode? PinCode { get; set; } = default!;

        public long? DistrictId { get; set; } = default!;

        public District? District { get; set; } = default!;

        [Display(Name = "Bank Name")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "BankName must be between 3 and 50 characters.")]
        public string? BankName { get; set; } = default!;

        [Display(Name = "Bank Account Number")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "BankAccountNumber must be between 3 and 50 characters.")]
        public string? BankAccountNumber { get; set; } = default!;

        [Display(Name = "IFSC code")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Code must be between 6 and 15 characters.")]
        public string? IFSCCode { get; set; } = default!;

        [Display(Name = "Agreement date")]
        [DataType(DataType.Date)]
        public DateTime? AgreementDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Activated date")]
        [DataType(DataType.Date)]
        public DateTime? ActivatedDate { get; set; } = DateTime.UtcNow;

        public VendorStatus? Status { get; set; }
        public Domain? DomainName { get; set; } = Domain.com;

        [Display(Name = "Document")]
        public string? DocumentUrl { get; set; } = default!;

        [Display(Name = "Document")]
        public string? DocumentImageExtension { get; set; } = default!;

        public string? AddressMapLocation { get; set; }
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }

        public List<ApplicationUser>? ApplicationUser { get; set; }

        public List<ClientCompany>? Clients { get; set; } = new List<ClientCompany>();

        public bool Deleted { get; set; } = false;

        public int? RateCount
        {
            get { return Ratings != null ? Ratings.Count : 0; }
        }

        public int? RateTotal
        {
            get
            {
                return (Ratings != null ? Ratings.Sum(m => m.Rate) : 0);
            }
        }

        public virtual ICollection<AgencyRating>? Ratings { get; set; }
        public string? MobileAppUrl { get; set; } = EnvHelper.Get("APP_URL");
        public bool CanChangePassword { get; set; } = true;
        public bool HasClaims { get; set; } = false;

        [NotMapped]
        public bool SelectedByCompany { get; set; }

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

        [NotMapped]
        public int UserCount { get; set; }

        [NotMapped]
        public int CurrentCasesCount { get; set; }

        [NotMapped]
        public int CompletedCasesCount { get; set; }

        public override string ToString()
        {
            return $"Investigation Agency Information:\n" +
           $"- Domain (web) name: {DomainName}\n" +
           $"- Name: {Name}\n" +
           $"- Domain name: {Email}\n" +
           $"- Address line: {Addressline}\n" +
           $"- City: {District}\n" +
           $"- State: {State}\n" +
           $"- Country: {Country}\n" +
           $"- Investigation Service Types: {VendorInvestigationServiceTypes}\n" +
           $"- Bank Name: {BankName}\n" +
           $"- Bank Account Number: {BankAccountNumber}\n" +
           $"- IFSC Code: {IFSCCode}\n" +
           $"- Agreement Date: {AgreementDate}\n" +
           $"- Activated Date: {ActivatedDate}";
        }
    }

    public enum VendorStatus
    {
        ACTIVE,
        INACTIVE
    }
}