using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class PolicyDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PolicyDetailId { get; set; }

        [Display(Name = "Investigation type")]
        [Required]
        public long InvestigationServiceTypeId { get; set; } = default!;

        [Display(Name = "Investigation type")]
        public InvestigationServiceType? InvestigationServiceType { get; set; } = default!;

        [Display(Name = "Case number")]
        [Required]
        [StringLength(20)]
        public string ContractNumber { get; set; } = default!;

        [Display(Name = "Case issue date")]
        [DataType(DataType.Date)]
        [Required]
        public DateTime ContractIssueDate { get; set; }

        public InsuranceType? InsuranceType { get; set; } = Models.InsuranceType.CLAIM;

        [Display(Name = "Date of incident")]
        [DataType(DataType.Date)]
        [Required]
        public DateTime DateOfIncident { get; set; }

        [Display(Name = "Cause of loss")]
        [Required]
        [StringLength(70)]
        public string CauseOfLoss { get; set; }

        [Display(Name = "Sum assured value")]
        [Column(TypeName = "decimal(15,2)")]
        [Range(100, 9999999999)]
        public decimal SumAssuredValue { get; set; }

        [Display(Name = "Budget centre")]
        [Required]
        public long CostCentreId { get; set; }

        [Display(Name = "Budget centre")]
        public CostCentre? CostCentre { get; set; }

        [Required]
        public long CaseEnablerId { get; set; }

        [Display(Name = "Reason To Verify")]
        public CaseEnabler? CaseEnabler { get; set; }

        [Display(Name = "Case Document")]
        public string? DocumentImageExtension { get; set; }
        public string? DocumentPath { get; set; }
        public byte[]? DocumentImage { get; set; } = default!;

        [Display(Name = "Case remarks")]
        [StringLength(500)]
        public string? Comments { get; set; }
        [Display(Name = "Case Document")]

        #region NOT MAPPED PROPERTIES
        [NotMapped]
        [Required]
        [FileExtensions(Extensions = "jpg,jpeg,png")]
        public IFormFile? Document { get; set; }
        #endregion
        public override string ToString()
        {
            return $"Case Information:\n" +
           $"- Contract Number: {ContractNumber}\n" +
           $"- Investigation Service Type: {InvestigationServiceType}\n" +
           $"- Case Issue Date: ${ContractIssueDate}\n" +
           $"- Date Of Incident: {DateOfIncident}\n" +
           $"- Cause Of Loss: {CauseOfLoss}\n" +
           $"- Sum Assured Value: {SumAssuredValue}\n" +
           $"- Cost Centre: {CostCentre}\n" +
           $"- Case Enabler: {CaseEnabler}\n" +
           $"- Remarks: {Comments}";
        }
    }
}