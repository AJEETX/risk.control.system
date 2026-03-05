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

        [Display(Name = "Agency name")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [StringLength(70, MinimumLength = 3, ErrorMessage = "Addressline must be between 3 and 70 characters.")]
        public string Addressline { get; set; } = string.Empty;

        public List<VendorInvestigationServiceType>? VendorInvestigationServiceTypes { get; set; } = default!;

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

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; } = default!;

        [Display(Name = "Bank Account Number")]
        public string? BankAccountNumber { get; set; } = default!;

        [Display(Name = "IFSC code")]
        public string? IFSCCode { get; set; } = default!;

        [Display(Name = "Agreement date")]
        [DataType(DataType.Date)]
        public DateTime? AgreementDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Activated date")]
        [DataType(DataType.Date)]
        public DateTime? ActivatedDate { get; set; } = DateTime.UtcNow;

        public VendorStatus? Status { get; set; } = VendorStatus.ACTIVE;
        public Domain? DomainName { get; set; } = Domain.com;

        [Display(Name = "Document")]
        public string? DocumentUrl { get; set; } = default!;

        [Display(Name = "Document")]
        public string? DocumentImageExtension { get; set; } = default!;

        public string? AddressMapLocation { get; set; }
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }

        [Display(Name = "Vendor Users")]
        public List<ApplicationUser>? ApplicationUser { get; set; }

        [Display(Name = "Insurer names")]
        public List<ClientCompany>? Clients { get; set; } = new List<ClientCompany>();

        [Display(Name = "Empanel")]
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
        public bool CanChangePassword { get; set; } = false;
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

        public override string ToString()
        {
            return $"Investigation Agency Information:\n" +
           $"- Domain (web) name: {DomainName}\n" +
           $"- Name: {Name}\n" +
           $"- Phone Number: ${PhoneNumber}\n" +
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
           $"- Activated Date: {ActivatedDate}\n" +
           $"- Status: {Status.GetEnumDisplayName()}";
        }
    }

    public enum VendorStatus
    {
        ACTIVE,
        INACTIVE,
        DELIST
    }
}