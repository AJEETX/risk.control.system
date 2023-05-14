using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ClaimsInvestigation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimsInvestigationCaseId { get; set; } = Guid.NewGuid().ToString();

        [Display(Name = "Company name")]
        public string? ClientCompanyId { get; set; }
        [Display(Name = "Company name")]
        public ClientCompany? ClientCompany { get; set; }

        [NotMapped]
        public bool HasClientCompany { get; set; } = true;
        public List<Vendor>? Vendors { get; set; }
        [Display(Name = "Contract number")]
        public string ContractNumber { get; set; } = default!;
        public string Description { get; set; } = default!;
        [Display(Name = "Line of Business")]
        public string? LineOfBusinessId { get; set; } = default!;
        [Display(Name = "Line of Business")]
        public LineOfBusiness? LineOfBusiness { get; set; } = default!;
        [Display(Name = "Investigation type")]
        public string? InvestigationServiceTypeId { get; set; } = default!;
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
        [Display(Name = "Case issue date")]
        public DateTime? ContractIssueDate { get; set; }
        [Display(Name = "Customer name")]
        public string? CustomerName { get; set; }
        [Display(Name = "Customer date of birth")]
        public DateTime? CustomerDateOfBirth { get; set; }
        [Display(Name = "Customer contact number")]
        public long ContactNumber { get; set; }
        [Display(Name = "Claim type")]
        public ClaimType ClaimType { get; set; }
        [Display(Name = "Date of incident")]
        public DateTime? DateOfIncident { get; set; }

        [Display(Name = "Cause of loss")]
        public string? CauseOfLoss { get; set; }
        public Gender Gender { get; set; }
        [Display(Name = "Sum assured value")]
        public int? SumAssuredValue { get; set; }
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
        [Display(Name = "Customer income")]
        public int? CustomerIncome { get; set; }
        [Display(Name = "Customer occupation")]
        public string? CustomerOccupation { get; set; }
        [Display(Name = "Customer education")]
        public string? CustomerEducation { get; set; }
        [Display(Name = "Beneficiary name")]
        public string? BeneficiaryName { get; set; }

        [Display(Name = "Beneficiary relation")]
        public string? BeneficiaryRelationId { get; set; }
        [Display(Name = "Beneficiary relation")]
        public BeneficiaryRelation? BeneficiaryRelation { get; set; }
        [Display(Name = "Beneficiary contact number")]
        public int? BeneficiaryContactNumber { get; set; }
        [Display(Name = "Beneficiary income")]
        public int? BeneficiaryIncome { get; set; }
        [Display(Name = "Customer type")]
        public CustomerType? CustomerType { get; set; }
        [Display(Name = "Cost centre")]
        public string? CostCentreId { get; set; }
        [Display(Name = "Cost centre")]
        public CostCentre? CostCentre { get; set; }
        public string? CaseEnablerId { get; set; }
        [Display(Name = "Case enabler")]
        public CaseEnabler? CaseEnabler { get; set; }
        public List<FileAttachment>? Attachments { get; set; }
        public string? Comments { get; set; }
        [NotMapped]
        public bool SelectedToAssign { get; set; }
        public string? CurrentUserId { get; set; }
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
