using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseLocation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CaseLocationId { get; set; }
        [Display(Name = "Beneficiary name")]
        public string? BeneficiaryName { get; set; }

        [Display(Name = "Beneficiary relation")]
        public long? BeneficiaryRelationId { get; set; }
        [Display(Name = "Beneficiary relation")]
        public BeneficiaryRelation? BeneficiaryRelation { get; set; }
        [Display(Name = "Beneficiary contact number")]
        public long? BeneficiaryContactNumber { get; set; }
        [Display(Name = "Beneficiary income")]

        [Column(TypeName = "decimal(15,2)")]
        public decimal? BeneficiaryIncome { get; set; }
        [Display(Name = "Country name")]
        public string? CountryId { get; set; } = default!;
        public Country? Country { get; set; } = default!;
        [Display(Name = "State")]
        public string? StateId { get; set; } = default!;
        public State? State { get; set; } = default!;

        [Display(Name = "District")]
        public string? DistrictId { get; set; } = default!;
        public District? District { get; set; } = default!;
        [Display(Name = "PinCode name")]
        public string? PinCodeId { get; set; } = default!;
        [Display(Name = "PinCode name")]
        public PinCode? PinCode { get; set; } = default!;

        [Display(Name = "Address line1")]
        public string? Addressline { get; set; }
        [Display(Name = "Address line1")]
        public string? Addressline2 { get; set; }
        public string ClaimsInvestigationId { get; set; }
        public ClaimsInvestigation ClaimsInvestigation { get; set; } = default!;

    }
}
