using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace risk.control.system.Models
{
    public class ClaimsInvestigation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimsInvestigationId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Company name")]
        public string ClientCompanyId { get; set; }

        [Display(Name = "Company name")]
        public ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Agency name")]
        public string? VendorId { get; set; }

        [Display(Name = "Agency name")]
        public Vendor? Vendor { get; set; }

        [NotMapped]
        public bool HasClientCompany { get; set; } = true;

        public List<Vendor>? Vendors { get; set; } = new();

        [Display(Name = "Contract number")]
        public string ContractNumber { get; set; } = default!;

        [Display(Name = "Claim Comments")]
        public string Description { get; set; } = default!;

        [Required]
        [Display(Name = "Line of Business")]
        public string LineOfBusinessId { get; set; } = default!;

        [Display(Name = "Line of Business")]
        public LineOfBusiness? LineOfBusiness { get; set; } = default!;

        [Required]
        [Display(Name = "Investigation type")]
        public string InvestigationServiceTypeId { get; set; } = default!;

        [Display(Name = "Investigation type")]
        public InvestigationServiceType? InvestigationServiceType { get; set; } = default!;

        [Display(Name = "Case status")]
        public string? InvestigationCaseStatusId { get; set; } = default!;

        [Display(Name = "Case status")]
        public InvestigationCaseStatus? InvestigationCaseStatus { get; set; } = default!;

        [Display(Name = "Case sub status")]
        public string? InvestigationCaseSubStatusId { get; set; } = default!;

        [Display(Name = "Case sub status")]
        public InvestigationCaseSubStatus? InvestigationCaseSubStatus { get; set; } = default!;

        [Required]
        [Display(Name = "Case issue date")]
        [DataType(DataType.Date)]
        public DateTime ContractIssueDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Customer name")]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime CustomerDateOfBirth { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Customer contact number")]
        [DataType(DataType.PhoneNumber)]
        public long ContactNumber { get; set; }

        [Required]
        [Display(Name = "Claim type")]
        public ClaimType? ClaimType { get; set; }

        [Required]
        [Display(Name = "Date of incident")]
        [DataType(DataType.Date)]
        public DateTime DateOfIncident { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Cause of loss")]
        public string CauseOfLoss { get; set; }

        public Gender? Gender { get; set; }

        [Required]
        [Display(Name = "Sum assured value")]
        [Column(TypeName = "decimal(15,2)")]
        public decimal SumAssuredValue { get; set; }

        [Required]
        [Display(Name = "Address line")]
        public string Addressline { get; set; }

        [Required]
        [Display(Name = "PinCode name")]
        public string PinCodeId { get; set; } = default!;

        [Display(Name = "PinCode name")]
        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "State name")]
        [Required]
        public string StateId { get; set; } = default!;

        [Display(Name = "State name")]
        public State? State { get; set; } = default!;

        [Required]
        [Display(Name = "Country name")]
        public string CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country? Country { get; set; } = default!;

        [Required]
        [Display(Name = "District")]
        public string DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;

        [Required]
        [Display(Name = "Customer type")]
        public CustomerType? CustomerType { get; set; }

        [Required]
        [Display(Name = "Budget centre")]
        public string CostCentreId { get; set; }

        [Display(Name = "Budget centre")]
        public CostCentre? CostCentre { get; set; }

        [Required]
        public string CaseEnablerId { get; set; }

        [Display(Name = "Reason To Verify")]
        public CaseEnabler? CaseEnabler { get; set; }

        [Display(Name = "Claim Document")]
        [NotMapped]
        public IFormFile? Document { get; set; }

        [Display(Name = "Claim Document")]
        public byte[]? DocumentImage { get; set; } = default!;

        [Display(Name = "Claim remarks")]
        public string? Comments { get; set; }

        [NotMapped]
        public bool SelectedToAssign { get; set; }

        public string? CurrentUserEmail { get; set; }
        public List<CaseLocation>? CaseLocations { get; set; }

        public override string ToString()
        {
            return $"Case Id: {ClaimsInvestigationId}, <br /> ClaimType: {ClaimType}";
        }

        [Required]
        [Display(Name = "Customer income")]
        public Income? CustomerIncome { get; set; }

        [Required]
        [Display(Name = "Customer occupation")]
        public Occupation? CustomerOccupation { get; set; }

        [Required]
        [Display(Name = "Customer education")]
        public Education? CustomerEducation { get; set; }

        [FileExtensions(Extensions = "jpg,jpeg,png")]
        public string? ProfilePictureUrl { get; set; }

        public byte[]? ProfilePicture { get; set; }

        [Display(Name = "Image")]
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        public bool IsReviewCase { get; set; } = false;
        public bool IsReady2Assign { get; set; } = false;
    }

    public enum Income
    {
        [Display(Name = "UNKNOWN")]
        UNKNOWN,

        [Display(Name = "0.0 Lac")]
        NO_INCOME,

        [Display(Name = "0 - 2.5 Lac")]
        TAXFREE_SLOT,

        [Display(Name = "2.5 - 5 Lac")]
        BASIC_INCOME,

        [Display(Name = "5 - 8 Lac")]
        MEDIUUM_INCOME,

        [Display(Name = "8 - 15 Lac")]
        UPPER_INCOME,

        [Display(Name = "15 - 30 Lac")]
        HIGHER_INCOME,

        [Display(Name = "30 - 50 Lac")]
        TOP_HIGHER_INCOME,

        [Display(Name = "50 + Lac")]
        PREMIUM_INCOME,
    }

    public enum Occupation
    {
        UNKNOWN,

        [Display(Name = "UNEMPLOYED")]
        UNEMPLOYED,

        [Display(Name = "DOCTOR")]
        DOCTOR,

        [Display(Name = "ENGINEER")]
        ENGINEER,

        [Display(Name = "ACCOUNTANT")]
        ACCOUNTANT,

        [Display(Name = "SELF EMPLOYED")]
        SELF_EMPLOYED,

        [Display(Name = "OTHER")]
        OTHER
    }

    public enum Education
    {
        [Display(Name = "PRIMARY SCHOOL")]
        PRIMARY_SCHOOL,

        [Display(Name = "HIGH SCHOOL")]
        HIGH_SCHOOL,

        [Display(Name = "12th Class")]
        CLASS_12,

        [Display(Name = "GRADUATE")]
        GRADUATE,

        [Display(Name = "POST GRADUATE")]
        POST_GRADUATE,

        [Display(Name = "PROFESSIONAL")]
        PROFESSIONAL,
    }

    public class VerificationLocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VerificationLocationId { get; set; } = Guid.NewGuid().ToString();

        public virtual ClaimsInvestigation ClaimsInvestigation { get; set; } = default!;

        [Display(Name = "Address line")]
        public string? Addressline { get; set; }

        [Display(Name = "PinCode name")]
        public string? PinCodeId { get; set; } = default!;

        [Display(Name = "PinCode name")]
        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "State name")]
        public string? StateId { get; set; } = default!;

        [Display(Name = "State name")]
        public State? State { get; set; } = default!;

        [Required]
        [Display(Name = "Country name")]
        public string CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country Country { get; set; } = default!;

        [Display(Name = "District")]
        public string? DistrictId { get; set; } = default!;

        [Display(Name = "District")]
        public District? District { get; set; } = default!;
    }

    public enum ClaimType
    {
        [Display(Name = "Death")]
        DEATH,

        [Display(Name = "Health")]
        HEALTH
    }

    public enum Gender
    {
        [Display(Name = "Male")]
        MALE,

        [Display(Name = "Female")]
        FEMALE,

        [Display(Name = "Other")]
        OTHER
    }

    public enum CustomerType
    {
        [Display(Name = "HNI")]
        HNI,

        [Display(Name = "Non-NHI")]
        NONHNI,
    }
}